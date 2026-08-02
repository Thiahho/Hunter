namespace Hunter.Infrastructure.Messaging;

public class WhatsAppCloudApiOptions
{
    public const string SectionName = "WhatsAppCloudApi";

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string ApiVersion { get; set; } = "v21.0";

    // MVP de un solo tenant por número de WhatsApp: el payload del webhook de Meta no trae
    // OrganizationId, así que lo resolvemos acá. Multi-número/multi-org queda para V2.
    public int? OrganizationId { get; set; }

    // Requerido por Meta para validar la firma HMAC de los webhooks entrantes (X-Hub-Signature-256).
    public string? AppSecret { get; set; }

    // Token elegido por nosotros y configurado también en Meta al dar de alta el webhook (handshake GET).
    public string? WebhookVerifyToken { get; set; }

    // Meta exige "template" para mensajes que abren conversación (fuera de la ventana de 24hs de servicio).
    // Sin una plantilla aprobada por Meta, solo se puede enviar tipo "text" (responde dentro de la ventana).
    public string? TemplateName { get; set; }
    public string TemplateLanguage { get; set; } = "es";

    // Algunas plantillas se aprueban sin variables (texto 100% estático, ej. "bienvenida_general").
    // Meta rechaza el envío con (#132000) si el número de parámetros no coincide EXACTO con lo
    // aprobado, así que hay que poder desactivar el parámetro del body por plantilla.
    public bool TemplateHasBodyParameter { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
