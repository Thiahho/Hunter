namespace Hunter.Infrastructure.BackgroundJobs;

public class ProspectDriveSyncOptions
{
    public const string SectionName = "ProspectDriveSync";

    // Cada cuántos minutos se regenera y sube el Excel de todos los prospectos activos. 30 min
    // por defecto: no hace falta más frecuencia que eso para que el equipo lo vea "al día", y
    // evita reescribir el archivo en Drive constantemente si hay mucha carga de prospectos.
    public int PollIntervalMinutes { get; set; } = 30;
}
