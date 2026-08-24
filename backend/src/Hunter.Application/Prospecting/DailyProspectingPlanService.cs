using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Prospecting;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

// Reparte un pool grande de localidades (sin el tope de 5 que rige una sola
// ScheduledProspectAutomation) en varias automatizaciones escalonadas a lo largo del día,
// combinando OpenStreetMap y Apify, para acercarse a un volumen diario que una sola corrida
// (tope 300 en OSM, 100 en Apify) no puede alcanzar — ver ScheduledProspectAutomationService
// para el tope por corrida de cada fuente. Reusa CreateAsync en vez de escribir
// ScheduledProspectAutomation directamente: así la resolución de campaña y las validaciones por
// fuente no se duplican acá.
//
// Rubros fijos a propósito (mismo criterio "a pedido" que ProspectSearchPage.tsx): Casa de
// repuestos + el keyword "mayorista suspensión tren delantero", igual que lo único que hoy se
// puede elegir a mano en la búsqueda manual.
public class DailyProspectingPlanService(IScheduledProspectAutomationService automationService) : IDailyProspectingPlanService
{
    private const int MaxLocalitiesPerOsmBatch = 5;
    private const int MaxLocalitiesPerApifyBatch = 5;
    private const int OsmMaxResultsPerBatch = 300;
    private const int ApifyMaxResultsPerBatch = 100;

    private const string WholesaleKeyword = "mayorista suspensión tren delantero";
    private static readonly IReadOnlyCollection<ProspectCategory> OsmCategories = [ProspectCategory.AutoPartsStore];
    private static readonly IReadOnlyCollection<string> OsmKeywords = [WholesaleKeyword];
    private static readonly IReadOnlyCollection<string> ApifyKeywords = ["casa de repuestos", WholesaleKeyword];

    public async Task<Result<DailyProspectingPlanDto>> CreateAsync(CreateDailyProspectingPlanRequest request, CancellationToken ct = default)
    {
        var localities = (request.Localities ?? [])
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l!)
            .Distinct()
            .ToList();

        if (localities.Count == 0)
            return Result<DailyProspectingPlanDto>.Failure("Debe indicar al menos una zona o localidad.");

        if (request.StartAt <= DateTimeOffset.UtcNow)
            return Result<DailyProspectingPlanDto>.Failure("La fecha y hora de inicio debe ser en el futuro.");

        var interval = TimeSpan.FromMinutes(Math.Max(5, request.IntervalMinutes));

        var osmChunks = localities.Chunk(MaxLocalitiesPerOsmBatch).ToList();
        var apifyChunks = request.IncludeApify ? localities.Chunk(MaxLocalitiesPerApifyBatch).ToList() : [];

        var created = new List<ScheduledProspectAutomationDto>();
        var scheduledAt = request.StartAt;
        var estimatedCeiling = 0;

        foreach (var chunk in osmChunks)
        {
            var result = await automationService.CreateAsync(
                new ScheduleProspectAutomationRequest(
                    chunk, OsmCategories, request.RadiusKm, OsmMaxResultsPerBatch, scheduledAt, OsmKeywords,
                    ProspectAutomationSource.OpenStreetMap),
                ct);

            if (result.Succeeded)
            {
                created.Add(result.Value!);
                estimatedCeiling += OsmMaxResultsPerBatch;
            }

            scheduledAt += interval;
        }

        foreach (var chunk in apifyChunks)
        {
            var result = await automationService.CreateAsync(
                new ScheduleProspectAutomationRequest(
                    chunk, null, 0, ApifyMaxResultsPerBatch, scheduledAt, ApifyKeywords,
                    ProspectAutomationSource.Apify),
                ct);

            if (result.Succeeded)
            {
                created.Add(result.Value!);
                estimatedCeiling += ApifyMaxResultsPerBatch;
            }

            scheduledAt += interval;
        }

        if (created.Count == 0)
            return Result<DailyProspectingPlanDto>.Failure("No se pudo programar ninguna corrida (ver plantilla de WhatsApp activa u otros errores de validación).");

        return Result<DailyProspectingPlanDto>.Success(new DailyProspectingPlanDto(created, estimatedCeiling, localities.Count));
    }
}
