namespace Hunter.Application.Prospecting.Contracts;

// Localities es el pool completo (sin el tope de 5 que rige una sola automatización): el
// servicio lo reparte en chunks de a 5 y programa una ScheduledProspectAutomation por chunk,
// escalonadas cada IntervalMinutes desde StartAt — ver DailyProspectingPlanService.
public record CreateDailyProspectingPlanRequest(
    IReadOnlyCollection<string> Localities,
    DateTimeOffset StartAt,
    int IntervalMinutes = 20,
    int RadiusKm = 10,
    bool IncludeApify = true);

// EstimatedCeiling = techo teórico antes de duplicados (cantidad de corridas × tope por corrida
// de cada fuente), no una promesa de contactos netos — ver ProspectDuplicateFinder.
public record DailyProspectingPlanDto(
    IReadOnlyCollection<ScheduledProspectAutomationDto> Automations,
    int EstimatedCeiling,
    int LocalitiesCovered);
