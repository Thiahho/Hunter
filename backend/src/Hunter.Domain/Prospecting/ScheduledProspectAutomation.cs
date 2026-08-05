using Hunter.Domain.Common;

namespace Hunter.Domain.Prospecting;

// Automatización de un solo disparo (doc de la feature "programar búsqueda + envío"): a la hora
// programada, el background service busca en OpenStreetMap con estos criterios, importa TODOS
// los prospectos válidos sin revisión humana, los suma a CampaignId y arranca/continúa el envío
// — ver ScheduledProspectAutomationService.RunAsync. Deliberadamente sin recurrencia (V1): cada
// fila es una ejecución puntual, no un cron.
public class ScheduledProspectAutomation : Entity
{
    public int OrganizationId { get; set; }
    public int CreatedByUserId { get; set; }

    // JSON de OpenStreetMapImportRequest (localidades, rubros, radio, máximo de resultados) —
    // mismo patrón que ImportBatchRecord.RawData/NormalizedData: serializar el contrato entero
    // en vez de columnas sueltas evita una migración nueva cada vez que ese contrato cambie.
    public string SearchCriteriaJson { get; set; } = null!;

    public int CampaignId { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }
    public ScheduledAutomationStatus Status { get; set; } = ScheduledAutomationStatus.Pending;

    public DateTimeOffset? RunAt { get; set; }
    public string? ResultSummary { get; set; }
}

public enum ScheduledAutomationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
