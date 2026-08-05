namespace Hunter.Infrastructure.BackgroundJobs;

public class CampaignQueueOptions
{
    public const string SectionName = "CampaignQueue";

    // Cada cuántos segundos el background service busca campañas Running y procesa un lote.
    public int PollIntervalSeconds { get; set; } = 60;

    // false por defecto: hasta que alguien lo habilite explícitamente (env var), el envío
    // sigue siendo 100% manual vía POST /campaigns/{id}/process-queue, como hasta ahora.
    public bool Enabled { get; set; }
}
