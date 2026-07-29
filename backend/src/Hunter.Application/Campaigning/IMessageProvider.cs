using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning;

public record SendMessageRequest(MessagingChannel Channel, string ToContact, string Content);

public record SendMessageResult(bool Success, string? ExternalMessageId, string? Error);

// Abstracción de proveedor de mensajería (doc 07 Epic07 P1: "la aplicación no debe
// depender directamente del proveedor"). Todavía no hay decisión tomada sobre
// WhatsApp oficial/BSP (doc 12, sección 27) — Infrastructure registra una
// implementación stub hasta que esa decisión se cierre.
public interface IMessageProvider
{
    string ProviderName { get; }

    Task<SendMessageResult> SendAsync(SendMessageRequest request, CancellationToken ct = default);
}
