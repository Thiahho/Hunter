namespace Hunter.Infrastructure.BackgroundJobs;

public class StaleLeadEscalationOptions
{
    public const string SectionName = "StaleLeadEscalation";

    // Cada cuántos segundos se revisan leads abiertos sin actividad. 30 min por defecto: no hace
    // falta más frecuencia que eso para un recordatorio, y evita spamear la API de Telegram.
    public int PollIntervalSeconds { get; set; } = 1800;
}
