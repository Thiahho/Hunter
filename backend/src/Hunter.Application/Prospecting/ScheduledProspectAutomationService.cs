using System.Text.Json;
using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

public class ScheduledProspectAutomationService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    IImportService importService,
    ICampaignService campaignService) : IScheduledProspectAutomationService
{
    private const int MaxLocalities = 5;
    private const int MinRadiusKm = 1;
    private const int MaxRadiusKm = 50;

    // Entre lotes de envío del drain loop de RunAsync: mismo espaciado que usa
    // CampaignQueueBackgroundService entre ticks, para no violar MessagesPerMinute de la campaña.
    private static readonly TimeSpan DelayBetweenSendBatches = TimeSpan.FromSeconds(60);

    public async Task<Result<ScheduledProspectAutomationDto>> CreateAsync(ScheduleProspectAutomationRequest request, CancellationToken ct = default)
    {
        var localities = (request.Localities ?? [])
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .Distinct()
            .ToList();

        if (localities.Count == 0)
            return Result<ScheduledProspectAutomationDto>.Failure("Debe indicar al menos una zona o localidad.");
        if (localities.Count > MaxLocalities)
            return Result<ScheduledProspectAutomationDto>.Failure($"Máximo {MaxLocalities} localidades por automatización.");

        if (request.RadiusKm < MinRadiusKm || request.RadiusKm > MaxRadiusKm)
            return Result<ScheduledProspectAutomationDto>.Failure($"El radio debe estar entre {MinRadiusKm} y {MaxRadiusKm} km.");

        if (request.ScheduledAt <= DateTimeOffset.UtcNow)
            return Result<ScheduledProspectAutomationDto>.Failure("La fecha y hora programada debe ser en el futuro.");

        var organizationId = currentUser.OrganizationId!.Value;

        var campaignIdResult = await ResolveDefaultCampaignAsync(organizationId, currentUser.UserId!.Value, ct);
        if (!campaignIdResult.Succeeded)
            return Result<ScheduledProspectAutomationDto>.Failure(campaignIdResult.Error!);

        var criteria = new OpenStreetMapImportRequest(localities, request.Categories, request.RadiusKm, request.MaxResults, request.Keywords);

        var automation = new ScheduledProspectAutomation
        {
            OrganizationId = organizationId,
            CreatedByUserId = currentUser.UserId!.Value,
            SearchCriteriaJson = JsonSerializer.Serialize(criteria),
            CampaignId = campaignIdResult.Value,
            ScheduledAt = request.ScheduledAt,
            Status = ScheduledAutomationStatus.Pending
        };

        db.ScheduledProspectAutomations.Add(automation);
        await db.SaveChangesAsync(ct);

        return Result<ScheduledProspectAutomationDto>.Success(await ToDtoAsync(automation, ct));
    }

    public async Task<IReadOnlyCollection<ScheduledProspectAutomationDto>> ListAsync(CancellationToken ct = default)
    {
        var automations = await db.ScheduledProspectAutomations
            .OrderByDescending(a => a.ScheduledAt)
            .ToListAsync(ct);

        var dtos = new List<ScheduledProspectAutomationDto>(automations.Count);
        foreach (var automation in automations)
            dtos.Add(await ToDtoAsync(automation, ct));

        return dtos;
    }

    public async Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default)
    {
        var automation = await db.ScheduledProspectAutomations.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (automation is null)
            return Result<bool>.Failure("Automatización no encontrada.");

        if (automation.Status != ScheduledAutomationStatus.Pending)
            return Result<bool>.Failure($"La automatización está en estado {automation.Status}, no se puede cancelar.");

        automation.Status = ScheduledAutomationStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    // Nunca lanza: cualquier fallo queda registrado en ResultSummary con Status=Failed en vez de
    // tumbar el background service que la llama (mismo criterio que InboundMessageService con
    // los canales de notificación).
    public async Task RunAsync(int id, CancellationToken ct = default)
    {
        var automation = await db.ScheduledProspectAutomations.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (automation is null || automation.Status != ScheduledAutomationStatus.Pending)
            return;

        automation.Status = ScheduledAutomationStatus.Running;
        automation.RunAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var criteria = JsonSerializer.Deserialize<OpenStreetMapImportRequest>(automation.SearchCriteriaJson)
                ?? throw new InvalidOperationException("SearchCriteriaJson corrupto o vacío.");

            var searchResult = await importService.ImportFromOpenStreetMapAsync(criteria, ct);
            if (!searchResult.Succeeded)
            {
                await FailAsync(automation, $"Búsqueda sin resultados: {searchResult.Error}", ct);
                return;
            }

            var batchId = searchResult.Value!.BatchId;

            // SelectedRecordIds = null → confirma TODOS los registros Valid, sin revisión humana
            // (ver ImportService.ConfirmAsync): es el comportamiento que ya usan CSV/Google
            // Places cuando no se manda una selección explícita, no un modo especial nuevo.
            var confirmResult = await importService.ConfirmAsync(batchId, null, ct);
            if (!confirmResult.Succeeded)
            {
                await FailAsync(automation, $"No se pudo confirmar la importación: {confirmResult.Error}", ct);
                return;
            }

            // Status == Imported (no solo "ProspectId != null"): los registros Duplicate también
            // tienen ProspectId seteado (apunta al prospecto ya existente, ver
            // ImportService.BuildRecordAsync), pero esos no pasaron por ConfirmAsync y no deben
            // sumarse a la campaña — si no se filtra por Status, un negocio ya cargado en una
            // corrida anterior se re-agrega como destinatario nuevo y recibe el mensaje de nuevo.
            var newProspectIds = await db.ImportBatchRecords
                .Where(r => r.ImportBatchId == batchId && r.Status == ImportBatchRecordStatus.Imported && r.ProspectId != null)
                .Select(r => r.ProspectId!.Value)
                .ToListAsync(ct);

            if (newProspectIds.Count == 0)
            {
                automation.Status = ScheduledAutomationStatus.Completed;
                automation.ResultSummary = "La búsqueda no encontró prospectos nuevos para importar (0 resultados válidos o todos duplicados).";
                await db.SaveChangesAsync(ct);
                return;
            }

            var addResult = await campaignService.AddRecipientsAsync(automation.CampaignId, new AddRecipientsRequest(newProspectIds), ct);
            if (!addResult.Succeeded)
            {
                // La campaña "de sistema" compartida puede haber arrancado (Running) entre que se
                // programó esta automatización y que le tocó correr — típico cuando se programan
                // varias fechas juntas y la primera ya mandó y arrancó la campaña que las demás
                // iban a reusar. Se resuelve la campaña vigente en este momento (reusa una libre o
                // crea una nueva) y se reintenta una vez antes de darla por fallida.
                var retryCampaignId = await ResolveDefaultCampaignAsync(automation.OrganizationId, automation.CreatedByUserId, ct);
                if (retryCampaignId.Succeeded)
                {
                    automation.CampaignId = retryCampaignId.Value;
                    addResult = await campaignService.AddRecipientsAsync(automation.CampaignId, new AddRecipientsRequest(newProspectIds), ct);
                }

                if (!addResult.Succeeded)
                {
                    await FailAsync(
                        automation,
                        $"Se importaron {newProspectIds.Count} prospectos, pero no se pudieron sumar a la campaña: {addResult.Error}",
                        ct);
                    return;
                }
            }

            // StartAsync falla si la campaña ya está Running (ej. otra automatización la arrancó
            // antes) — no es un error real acá, solo significa que ya estaba en marcha.
            var startResult = await campaignService.StartAsync(automation.CampaignId, ct);
            var startNote = startResult.Succeeded
                ? "Campaña iniciada."
                : $"Campaña no se pudo iniciar automáticamente ({startResult.Error}).";

            var (sent, failed, suppressed) = await DrainSendQueueAsync(automation.CampaignId, ct);

            automation.Status = ScheduledAutomationStatus.Completed;
            automation.ResultSummary =
                $"Importados {confirmResult.Value!.Created} prospectos, {addResult.Value!.Added} sumados a la campaña. {startNote} " +
                $"Envío: {sent} enviados, {failed} fallidos, {suppressed} suprimidos.";
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await FailAsync(automation, $"Error inesperado: {ex.Message}", ct);
        }
    }

    // Vacía la cola de la campaña respetando MessagesPerMinute (mismo mecanismo que
    // CampaignQueueBackgroundService, pero corrido acá mismo en vez de depender de que ese
    // servicio esté habilitado — CampaignQueue:Enabled es false por defecto, y esta
    // automatización tiene que poder mandar los mensajes sin que alguien prenda ese flag aparte).
    private async Task<(int Sent, int Failed, int Suppressed)> DrainSendQueueAsync(int campaignId, CancellationToken ct)
    {
        var messagesPerMinute = await db.Campaigns
            .Where(c => c.Id == campaignId)
            .Select(c => c.MessagesPerMinute)
            .FirstOrDefaultAsync(ct);

        var batchSize = Math.Max(1, messagesPerMinute);
        var totalSent = 0;
        var totalFailed = 0;
        var totalSuppressed = 0;

        // Tope defensivo: MaxResults de la búsqueda ya está acotado a 300 (OpenStreetMapClient),
        // así que esto nunca debería alcanzarse — es solo para que un bug no deje el loop girando
        // para siempre.
        for (var iteration = 0; iteration < 500; iteration++)
        {
            var result = await campaignService.ProcessQueueAsync(campaignId, batchSize, ct);
            if (!result.Succeeded || result.Value!.Processed == 0)
                break;

            totalSent += result.Value.Sent;
            totalFailed += result.Value.Failed;
            totalSuppressed += result.Value.Suppressed;

            if (result.Value.Processed < batchSize)
                break;

            await Task.Delay(DelayBetweenSendBatches, ct);
        }

        return (totalSent, totalFailed, totalSuppressed);
    }

    private async Task FailAsync(ScheduledProspectAutomation automation, string reason, CancellationToken ct)
    {
        automation.Status = ScheduledAutomationStatus.Failed;
        automation.ResultSummary = reason;
        await db.SaveChangesAsync(ct);
    }

    // Nombre fijo para poder encontrar y reusar siempre la misma campaña "de sistema" en vez de
    // crear una nueva por cada automatización programada — así el usuario nunca tiene que armar
    // una campaña a mano antes de poder programar. No toca campañas creadas manualmente: solo
    // busca/crea por este nombre exacto.
    private const string DefaultCampaignName = "Prospección automática (WhatsApp)";

    private async Task<Result<int>> ResolveDefaultCampaignAsync(int organizationId, int userId, CancellationToken ct)
    {
        var existing = await db.Campaigns
            .Where(c => c.OrganizationId == organizationId
                && c.Channel == MessagingChannel.Whatsapp
                && c.Name == DefaultCampaignName
                && (c.Status == CampaignStatus.Draft || c.Status == CampaignStatus.Ready || c.Status == CampaignStatus.Paused))
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return Result<int>.Success(existing.Id);

        // El contenido real que Meta entrega lo define WhatsAppCloudApi:TemplateName a nivel
        // organización (ver WhatsAppCloudApiMessageProvider), no esta plantilla — pero Campaign
        // igual exige un MessageTemplateId válido, así que se toma la única plantilla de WhatsApp
        // activa que no sea la de catálogo (esa es para la respuesta automática post-"Interesado",
        // no para el primer contacto).
        var candidateTemplates = await db.MessageTemplates
            .Where(t => t.OrganizationId == organizationId && t.Channel == MessagingChannel.Whatsapp && t.IsActive && !t.IsCatalogTemplate)
            .Select(t => t.Id)
            .ToListAsync(ct);

        if (candidateTemplates.Count == 0)
            return Result<int>.Failure(
                "No hay ninguna plantilla de WhatsApp activa para elegir automáticamente. Cargá una plantilla antes de programar.");
        if (candidateTemplates.Count > 1)
            return Result<int>.Failure(
                "Hay más de una plantilla de WhatsApp activa: no se puede elegir automáticamente cuál usar. Desactivá las que no correspondan.");

        var campaign = new Campaign
        {
            OrganizationId = organizationId,
            Name = DefaultCampaignName,
            Channel = MessagingChannel.Whatsapp,
            MessageTemplateId = candidateTemplates[0],
            CreatedBy = userId
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        return Result<int>.Success(campaign.Id);
    }

    private async Task<ScheduledProspectAutomationDto> ToDtoAsync(ScheduledProspectAutomation automation, CancellationToken ct)
    {
        var criteria = JsonSerializer.Deserialize<OpenStreetMapImportRequest>(automation.SearchCriteriaJson);
        var campaignName = await db.Campaigns
            .Where(c => c.Id == automation.CampaignId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? "(campaña eliminada)";

        return new ScheduledProspectAutomationDto(
            automation.Id,
            criteria?.Localities ?? [],
            criteria?.Categories,
            criteria?.RadiusKm ?? 0,
            criteria?.MaxResults ?? 0,
            automation.CampaignId,
            campaignName,
            automation.ScheduledAt,
            automation.Status,
            automation.RunAt,
            automation.ResultSummary,
            automation.CreatedAt,
            criteria?.Keywords);
    }
}
