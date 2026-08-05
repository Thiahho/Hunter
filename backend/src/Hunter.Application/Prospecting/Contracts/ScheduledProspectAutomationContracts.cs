using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

// Mismos criterios que OpenStreetMapImportRequest (localidades, rubros, radio, máximo) más la
// campaña destino y el momento en que tiene que correr. RadiusKm es obligatorio acá a propósito:
// el frontend (ProspectSearchPage) ya no ofrece el modo sin radio, ver esa página.
public record ScheduleProspectAutomationRequest(
    IReadOnlyCollection<string> Localities,
    IReadOnlyCollection<ProspectCategory>? Categories,
    int RadiusKm,
    int MaxResults,
    int CampaignId,
    DateTimeOffset ScheduledAt);

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
    DateTimeOffset CreatedAt);
