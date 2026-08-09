namespace Hunter.Application.Campaigning;

// Configuración del reintento automático cuando InboundMessageService detecta que la respuesta
// del prospecto vino de un auto-responder (ver IAutoReplyDetector). El envío en sí lo hace
// ScheduledMessageBackgroundService, igual que un "Programar mensaje" manual.
public class AutoReplyFollowUpOptions
{
    public const string SectionName = "AutoReplyFollowUp";

    public int DelayHours { get; set; } = 3;

    // Tope de nudges automáticos por prospecto (Prospect.AutoReplyAttempts). Agotado el tope se
    // deja de reintentar solo y el prospecto queda para revisión manual (Status AutoReplyDetected).
    public int MaxAutoReplyAttempts { get; set; } = 2;
}
