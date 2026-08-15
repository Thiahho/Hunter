using Hunter.Application.Common;
using Hunter.Domain.Crm;
using Hunter.Domain.Organizations;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Crm;

// Cuenta cuántos leads abiertos (New/InProgress) tiene una organización sin trabajar, y contra
// qué umbral (por organización, o el default) comparar — usado por
// ScheduledProspectAutomationService para frenar el envío de campañas nuevas en frío cuando el
// backlog de leads sin atender ya es demasiado grande (caso real Difrani, agosto 2026: 27 leads
// abiertos sin ninguna actividad mientras las tandas de envío seguían disparándose todos los
// días). Separado de ScheduledProspectAutomationService.RunAsync para poder testear esta regla
// sin tener que fakear IImportService/ICampaignService (auditoria.md, hallazgo Medio "el feature
// de backlog no tiene ningún test").
public static class OpenLeadBacklogGuard
{
    public const int DefaultThreshold = 15;

    public static async Task<(int Backlog, int Threshold)> EvaluateAsync(IHunterDbContext db, int organizationId, CancellationToken ct = default)
    {
        var backlog = await db.Leads
            .IgnoreQueryFilters()
            .Where(l => l.OrganizationId == organizationId && (l.Status == LeadStatus.New || l.Status == LeadStatus.InProgress))
            .CountAsync(ct);

        var configuredThreshold = await db.OrganizationSettings
            .IgnoreQueryFilters()
            .Where(s => s.OrganizationId == organizationId && s.Key == OrganizationSettingsKeys.OpenLeadBacklogThreshold)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        var threshold = int.TryParse(configuredThreshold, out var parsed) && parsed > 0 ? parsed : DefaultThreshold;

        return (backlog, threshold);
    }
}
