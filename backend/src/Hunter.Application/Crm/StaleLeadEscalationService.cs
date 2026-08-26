using Hunter.Application.Common;
using Hunter.Domain.Crm;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hunter.Application.Crm;

// Caso real Difrani (agosto 2026): 27 leads abiertos con 0 actividad registrada, algunos con más
// de 17 días de antigüedad, porque el único aviso que existía (NotifyAssigneeAsync en
// InboundMessageService) se manda una sola vez al crear el lead y nunca se repite. Este servicio
// vuelve a avisar por Telegram mientras el lead siga sin moverse.
public class StaleLeadEscalationService(
    IHunterDbContext db,
    ITelegramNotifier telegramNotifier,
    ILogger<StaleLeadEscalationService> logger) : IStaleLeadEscalationService
{
    // Default si la organización no configuró OrganizationSettingsKeys.StaleLeadEscalationHours.
    private const int DefaultStaleLeadEscalationHours = 24;

    public async Task<int> EscalateStaleLeadsAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var candidates = await db.Leads.IgnoreQueryFilters()
            .Where(l => (l.Status == LeadStatus.New || l.Status == LeadStatus.InProgress) && l.AssignedToUserId != null)
            .Select(l => new
            {
                l.Id,
                l.OrganizationId,
                l.ProspectId,
                AssignedToUserId = l.AssignedToUserId!.Value,
                l.CreatedAt,
                l.LastActivityAt,
                l.LastEscalatedAt
            })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return 0;

        var organizationIds = candidates.Select(c => c.OrganizationId).Distinct().ToList();
        var thresholdsByOrg = await db.OrganizationSettings.IgnoreQueryFilters()
            .Where(s => organizationIds.Contains(s.OrganizationId) && s.Key == OrganizationSettingsKeys.StaleLeadEscalationHours)
            .ToDictionaryAsync(s => s.OrganizationId, s => s.Value, ct);

        var escalatedLeadIds = new List<int>();

        foreach (var lead in candidates)
        {
            var thresholdHours = thresholdsByOrg.TryGetValue(lead.OrganizationId, out var raw)
                && int.TryParse(raw, out var configured) && configured > 0
                ? configured
                : DefaultStaleLeadEscalationHours;

            var reference = lead.LastActivityAt ?? lead.CreatedAt;
            var hoursSinceActivity = (now - reference).TotalHours;
            if (hoursSinceActivity < thresholdHours)
                continue;

            // No reenviar el recordatorio antes de que vuelva a cumplirse el umbral desde el
            // último aviso, para no spamear al vendedor en cada tick.
            if (lead.LastEscalatedAt is not null && (now - lead.LastEscalatedAt.Value).TotalHours < thresholdHours)
                continue;

            var sent = await TryNotifyAsync(lead.Id, lead.AssignedToUserId, lead.ProspectId, hoursSinceActivity, ct);
            if (sent)
                escalatedLeadIds.Add(lead.Id);
        }

        if (escalatedLeadIds.Count == 0)
            return 0;

        var leadsToStamp = await db.Leads.IgnoreQueryFilters()
            .Where(l => escalatedLeadIds.Contains(l.Id))
            .ToListAsync(ct);
        foreach (var lead in leadsToStamp)
            lead.LastEscalatedAt = now;

        await db.SaveChangesAsync(ct);
        return escalatedLeadIds.Count;
    }

    private async Task<bool> TryNotifyAsync(int leadId, int assignedToUserId, int prospectId, double hoursSinceActivity, CancellationToken ct)
    {
        try
        {
            var assignee = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == assignedToUserId, ct);
            if (assignee is null || string.IsNullOrWhiteSpace(assignee.TelegramChatId))
                return false;

            var prospect = await db.Prospects.IgnoreQueryFilters()
                .Include(p => p.Contacts)
                .FirstOrDefaultAsync(p => p.Id == prospectId, ct);
            if (prospect is null)
                return false;

            var days = (int)(hoursSinceActivity / 24);
            var sinceText = days >= 1 ? $"{days} día(s)" : $"{(int)hoursSinceActivity} hora(s)";
            var message = $"⏰ RECORDATORIO — lead sin actividad\n\n{prospect.BusinessName}\nLleva {sinceText} sin ningún movimiento. Revisalo antes de que se enfríe.";

            var whatsappContact = prospect.Contacts.FirstOrDefault(c => c.Channel == ProspectContactChannel.Whatsapp && c.IsActive);
            var button = whatsappContact is null ? null : LeadHandoffMessageBuilder.BuildWhatsAppButton(prospect, whatsappContact.Value, assignee.FirstName);

            var result = await telegramNotifier.SendAsync(assignee.TelegramChatId, message, button, ct);
            if (!result.Success)
            {
                logger.LogWarning(
                    "[LeadEscalation] No se pudo enviar el recordatorio de Telegram para el lead {LeadId}: {Error}",
                    leadId, result.Error);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[LeadEscalation] Error inesperado escalando el lead {LeadId}.", leadId);
            return false;
        }
    }
}
