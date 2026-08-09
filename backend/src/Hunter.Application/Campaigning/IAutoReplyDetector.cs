namespace Hunter.Application.Campaigning;

// Detecta si una respuesta entrante probablemente vino de un auto-responder de WhatsApp Business
// del prospecto (mensaje de bienvenida / fuera de horario) en vez de una persona. Se evalúa antes
// de IIntentClassifier: ver InboundMessageService.ProcessAsync.
public interface IAutoReplyDetector
{
    Task<bool> IsLikelyAutomatedAsync(int organizationId, string content, CancellationToken ct = default);
}
