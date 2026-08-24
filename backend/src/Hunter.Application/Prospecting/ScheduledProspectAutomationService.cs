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

    // Mismos topes que ImportService.MaxApifyLocalities/MaxApifyKeywords (duplicados acá igual
    // que MaxLocalities/MinRadiusKm/MaxRadiusKm ya duplican los de OSM — ver ImportService.cs).
    private const int MaxApifyLocalities = 5;
    private const int MaxApifyKeywords = 5;

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

        if (request.ScheduledAt <= DateTimeOffset.UtcNow)
            return Result<ScheduledProspectAutomationDto>.Failure("La fecha y hora programada debe ser en el futuro.");

        object criteria;
        if (request.Source == ProspectAutomationSource.Apify)
        {
            if (localities.Count > MaxApifyLocalities)
                return Result<ScheduledProspectAutomationDto>.Failure($"Máximo {MaxApifyLocalities} localidades por automatización.");

            var keywords = (request.Keywords ?? [])
                .Select(k => k?.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k!)
                .Distinct()
                .ToList();

            if (keywords.Count == 0)
                return Result<ScheduledProspectAutomationDto>.Failure("Debe indicar al menos un rubro para Apify.");
            if (keywords.Count > MaxApifyKeywords)
                return Result<ScheduledProspectAutomationDto>.Failure($"Máximo {MaxApifyKeywords} rubros por automatización.");

            criteria = new ApifyImportRequest(localities, keywords, request.MaxResults);
        }
        else
        {
            if (localities.Count > MaxLocalities)
                return Result<ScheduledProspectAutomationDto>.Failure($"Máximo {MaxLocalities} localidades por automatización.");

            if (request.RadiusKm < MinRadiusKm || request.RadiusKm > MaxRadiusKm)
                return Result<ScheduledProspectAutomationDto>.Failure($"El radio debe estar entre {MinRadiusKm} y {MaxRadiusKm} km.");

            criteria = new OpenStreetMapImportRequest(localities, request.Categories, request.RadiusKm, request.MaxResults, request.Keywords);
        }

        var organizationId = currentUser.OrganizationId!.Value;

        var campaignIdResult = await ResolveDefaultCampaignAsync(organizationId, currentUser.UserId!.Value, ct);
        if (!campaignIdResult.Succeeded)
            return Result<ScheduledProspectAutomationDto>.Failure(campaignIdResult.Error!);

        var automation = new ScheduledProspectAutomation
        {
            OrganizationId = organizationId,
            CreatedByUserId = currentUser.UserId!.Value,
            Source = request.Source,
            // criteria está tipado object acá (OpenStreetMapImportRequest o ApifyImportRequest
            // según Source): Serialize(criteria) con TValue inferido de "object" serializaría "{}"
            // por tipo estático, no por tipo real — hace falta pasar el Type explícito.
            SearchCriteriaJson = JsonSerializer.Serialize(criteria, criteria.GetType()),
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
            var searchResult = automation.Source switch
            {
                ProspectAutomationSource.Apify => await importService.ImportFromApifyAsync(
                    JsonSerializer.Deserialize<ApifyImportRequest>(automation.SearchCriteriaJson)
                        ?? throw new InvalidOperationException("SearchCriteriaJson corrupto o vacío."),
                    ct),
                _ => await importService.ImportFromOpenStreetMapAsync(
                    JsonSerializer.Deserialize<OpenStreetMapImportRequest>(automation.SearchCriteriaJson)
                        ?? throw new InvalidOperationException("SearchCriteriaJson corrupto o vacío."),
                    ct)
            };
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

            // El envío automático quedó deshabilitado (ver auditoria.md / decisión del equipo):
            // la automatización solo importa y suma prospectos a la campaña "de sistema", el
            // contacto real se hace a mano exportando a Excel (ver ProspectExportService) y
            // abriendo el link de wa.me correspondiente. La campaña queda en Draft/Ready, sin
            // arrancar, para no depender de una migración si en algún momento se retoma el envío
            // automático.
            automation.Status = ScheduledAutomationStatus.Completed;
            automation.ResultSummary =
                $"Importados {confirmResult.Value!.Created} prospectos, {addResult.Value!.Added} sumados a la campaña " +
                "(sin enviar: el contacto ahora se hace exportando a Excel).";
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            await FailAsync(automation, $"Error inesperado: {ex.Message}", ct);
        }
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
        var campaignName = await db.Campaigns
            .Where(c => c.Id == automation.CampaignId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? "(campaña eliminada)";

        IReadOnlyCollection<string> localities;
        IReadOnlyCollection<ProspectCategory>? categories;
        int radiusKm;
        int maxResults;
        IReadOnlyCollection<string>? keywords;

        if (automation.Source == ProspectAutomationSource.Apify)
        {
            var criteria = JsonSerializer.Deserialize<ApifyImportRequest>(automation.SearchCriteriaJson);
            localities = criteria?.Localities ?? [];
            categories = null;
            radiusKm = 0;
            maxResults = criteria?.MaxResults ?? 0;
            keywords = criteria?.Keywords;
        }
        else
        {
            var criteria = JsonSerializer.Deserialize<OpenStreetMapImportRequest>(automation.SearchCriteriaJson);
            localities = criteria?.Localities ?? [];
            categories = criteria?.Categories;
            radiusKm = criteria?.RadiusKm ?? 0;
            maxResults = criteria?.MaxResults ?? 0;
            keywords = criteria?.Keywords;
        }

        return new ScheduledProspectAutomationDto(
            automation.Id,
            localities,
            categories,
            radiusKm,
            maxResults,
            automation.CampaignId,
            campaignName,
            automation.ScheduledAt,
            automation.Status,
            automation.RunAt,
            automation.ResultSummary,
            automation.CreatedAt,
            keywords,
            automation.Source);
    }
}
