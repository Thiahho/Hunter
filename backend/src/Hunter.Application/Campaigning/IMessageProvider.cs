using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning;

// RecipientName es el valor dinámico para plantillas con parámetro de nombre (ej. {{1}} en
// "bienvenida_general"); si no se manda, se usa Content como fallback (compat con plantillas
// de un solo parámetro donde Content es el mensaje completo).
//
// PreferFreeText fuerza texto libre aunque haya una TemplateName configurada: para respuestas
// dentro de la ventana de servicio de 24hs (el prospecto/usuario interno ya escribió), donde
// texto libre es legal y preferible a repetir la plantilla de campaña.
//
// TemplateNameOverride + TemplateParameters permiten usar una plantilla distinta a la de
// campaña (ej. la de handoff a vendedores) con una cantidad de parámetros arbitraria, sin pasar
// por el switch 0/1/2 de WhatsAppCloudApiOptions.TemplateBodyParameterCount.
public record SendMessageRequest(
    MessagingChannel Channel,
    string ToContact,
    string Content,
    string? RecipientName = null,
    bool PreferFreeText = false,
    string? TemplateNameOverride = null,
    IReadOnlyList<string>? TemplateParameters = null);

public record SendMessageResult(bool Success, string? ExternalMessageId, string? Error);

// Abstracción de proveedor de mensajería (doc 07 Epic07 P1: "la aplicación no debe
// depender directamente del proveedor"). Todavía no hay decisión tomada sobre
// WhatsApp oficial/BSP (doc 12, sección 27) — Infrastructure registra una
// implementación stub hasta que esa decisión se cierre.
public interface IMessageProvider
{
    string ProviderName { get; }

    // Nombre de la plantilla UTILITY aprobada para notificar a un vendedor/administrativo
    // asignado (null = no configurada). Se expone acá, igual que ProviderName, para que
    // InboundMessageService pueda decidir plantilla-vs-texto-libre sin depender de
    // Hunter.Infrastructure (Application no referencia Infrastructure).
    string? HandoffTemplateName => null;

    Task<SendMessageResult> SendAsync(SendMessageRequest request, CancellationToken ct = default);
}
