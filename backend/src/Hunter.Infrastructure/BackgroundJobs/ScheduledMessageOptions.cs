namespace Hunter.Infrastructure.BackgroundJobs;

public class ScheduledMessageOptions
{
    public const string SectionName = "ScheduledMessage";

    // Cada cuántos segundos se fija si hay ScheduledMessage Pending vencidos (ScheduledAt <=
    // ahora). Sin flag "Enabled" a propósito, mismo criterio que ScheduledProspectAutomationOptions:
    // solo procesa filas que el usuario creó explícitamente con "Programar mensaje".
    public int PollIntervalSeconds { get; set; } = 30;
}
