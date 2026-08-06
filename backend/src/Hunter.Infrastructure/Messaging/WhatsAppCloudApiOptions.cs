namespace Hunter.Infrastructure.Messaging;

public class WhatsAppCloudApiOptions
{
    public const string SectionName = "WhatsAppCloudApi";

    public string? PhoneNumberId { get; set; }
    public string? AccessToken { get; set; }
    public string ApiVersion { get; set; } = "v21.0";

    // WhatsApp Business Account ID (distinto de PhoneNumberId). Solo se usa para consultar el
    // texto real aprobado de TemplateName vía Graph API (GET /{WabaId}/message_templates), así
    // el log de Mensajes muestra lo que Meta efectivamente entregó y no el texto libre local.
    // Sin configurar, el envío funciona igual: Message.Content cae de vuelta al texto local.
    public string? WabaId { get; set; }

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

    // Meta rechaza el envío con (#132000) si el número de parámetros no coincide EXACTO con lo
    // aprobado en la plantilla, así que hay que poder configurar cuántos espera cada una: 0
    // (texto 100% estático), 1 (solo el nombre del prospecto) o 2 (nombre + un valor fijo,
    // ej. "bienvenida_general": {{1}}=nombre del negocio, {{2}}=link al catálogo).
    public int TemplateBodyParameterCount { get; set; } = 1;

    // Segundo parámetro del body cuando TemplateBodyParameterCount = 2. Es un valor fijo por
    // organización (no varía por prospecto), ej. el link al catálogo.
    public string? TemplateSecondParameter { get; set; }

    // Payloads de los botones quick_reply de TemplateName, EN EL MISMO ORDEN en que Meta los
    // aprobó (index 0, 1, ...). Vacío = plantilla sin botones dinámicos. Hoy es un solo botón
    // genérico de interés ("Estoy interesado" -> QuickReplyPayloads.Interested), pero se deja
    // como lista (no un único string) para no tener que romper esta config si más adelante
    // se agrega algún otro botón.
    public IList<string> TemplateQuickReplyPayloads { get; set; } = [];

    // Plantilla UTILITY separada para notificar al vendedor/administrativo asignado
    // (SendMessageRequest.TemplateNameOverride). Necesaria porque el usuario interno nunca le
    // escribió al número del negocio: texto libre le falla con error 131047 fuera de la
    // ventana de 24hs. Vacío = se manda texto libre igual (falla en producción, sirve en dev).
    public string? HandoffTemplateName { get; set; }
    public string HandoffTemplateLanguage { get; set; } = "es";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PhoneNumberId) && !string.IsNullOrWhiteSpace(AccessToken);
}
