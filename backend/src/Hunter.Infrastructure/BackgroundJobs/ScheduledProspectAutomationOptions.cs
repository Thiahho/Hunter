namespace Hunter.Infrastructure.BackgroundJobs;

public class ScheduledProspectAutomationOptions
{
    public const string SectionName = "ScheduledProspectAutomation";

    // Cada cuántos segundos se fija si hay automatizaciones Pending vencidas (ScheduledAt <=
    // ahora). No tiene flag "Enabled" como CampaignQueueOptions a propósito: acá solo se procesan
    // filas que el usuario creó explícitamente con el botón "Programar", nunca campañas
    // preexistentes por accidente, así que no hace falta un interruptor de seguridad aparte.
    public int PollIntervalSeconds { get; set; } = 30;
}
