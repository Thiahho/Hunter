using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

// Mismos criterios que OpenStreetMapImportRequest (localidades, rubros, radio, máximo) más el
// momento en que tiene que correr. RadiusKm es obligatorio acá a propósito: el frontend
// (ProspectSearchPage) ya no ofrece el modo sin radio, ver esa página.
//
// Ya no pide CampaignId: la campaña destino se resuelve sola (ver
// ScheduledProspectAutomationService.ResolveDefaultCampaignAsync) reusando o creando una única
// campaña "Prospección automática (WhatsApp)" por organización, así el usuario no tiene que armar
// una campaña a mano antes de poder programar — total, el contenido real que Meta entrega ya lo
// define WhatsAppCloudApi:TemplateName a nivel organización, no el MessageTemplate de la campaña.
public record ScheduleProspectAutomationRequest(
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<ProspectCategory>? Categories,
    int RadiusKm,
    int MaxResults,
    DateTimeOffset ScheduledAt,
    IReadOnlyCollection<string>? Keywords = null);

public record ScheduledProspectAutomationDto(
    int Id,
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<ProspectCategory>? Categories,
    int RadiusKm,
    int MaxResults,
    int CampaignId,
    string CampaignName,
    DateTimeOffset ScheduledAt,
    ScheduledAutomationStatus Status,
    DateTimeOffset? RunAt,
    string? ResultSummary,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<string>? Keywords = null);
