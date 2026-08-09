namespace Hunter.Application.Campaigning;

// Configuración del reintento automático cuando InboundMessageService detecta que la respuesta
// del prospecto vino de un auto-responder (ver IAutoReplyDetector). El envío en sí lo hace
// ScheduledMessageBackgroundService, igual que un "Programar mensaje" manual.
public class AutoReplyFollowUpOptions
{
    public const string SectionName = "AutoReplyFollowUp";

    // En minutos (no horas) para poder ajustarlo fino en producción/testing (ej. 10 minutos)
    // sin quedar atado a números enteros de hora.
    public int DelayMinutes { get; set; } = 180;

    // Tope de nudges automáticos por prospecto (Prospect.AutoReplyAttempts). Agotado el tope se
    // deja de reintentar solo y el prospecto queda para revisión manual (Status AutoReplyDetected).
    public int MaxAutoReplyAttempts { get; set; } = 2;
}
