namespace Hunter.Application.Campaigning.Contracts;

// PreferFreeText: true cuando el caller sabe que está respondiendo dentro de la ventana de
// servicio de 24hs (ej. reintento de auto-reply, "Programar mensaje" sobre un prospecto que ya
// escribió) y por lo tanto no debe pisarse con la plantilla de campaña (WhatsAppCloudApi:TemplateName)
// aunque esté configurada. Default false: mantiene el comportamiento actual de "Mensaje de prueba",
// que puede mandarse a un prospecto que nunca escribió y necesita la plantilla aprobada.
public record SendTestMessageRequest(string Content, bool PreferFreeText = false);

public record TestMessageResultDto(int MessageId, bool Success, string? ExternalMessageId, string? Error);
