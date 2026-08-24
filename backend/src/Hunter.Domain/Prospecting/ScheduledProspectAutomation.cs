using Hunter.Domain.Common;

namespace Hunter.Domain.Prospecting;

// Automatización de un solo disparo (doc de la feature "programar búsqueda + envío"): a la hora
// programada, el background service busca (en OSM o Apify, según Source) con estos criterios,
// importa TODOS los prospectos válidos sin revisión humana y los suma a CampaignId — ver
// ScheduledProspectAutomationService.RunAsync. El envío automático de WhatsApp se cortó (el
// contacto ahora se hace a mano exportando a Excel), CampaignId queda solo para bookkeeping.
// Deliberadamente sin recurrencia (V1): cada fila es una ejecución puntual, no un cron.
public class ScheduledProspectAutomation : Entity
{
    public int OrganizationId { get; set; }
    public int CreatedByUserId { get; set; }

    public ProspectAutomationSource Source { get; set; } = ProspectAutomationSource.OpenStreetMap;

    // JSON de OpenStreetMapImportRequest o ApifyImportRequest según Source — mismo patrón que
    // ImportBatchRecord.RawData/NormalizedData: serializar el contrato entero en vez de columnas
    // sueltas evita una migración nueva cada vez que alguno de esos contratos cambie.
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

public enum ProspectAutomationSource
{
    OpenStreetMap,
    Apify
}
