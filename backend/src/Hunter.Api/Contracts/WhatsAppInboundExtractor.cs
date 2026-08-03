namespace Hunter.Api.Contracts;

// Mensaje entrante ya normalizado a partir del payload crudo de Meta, sin importar si vino
// como texto libre o como tap de un botón (plantilla o interactivo).
public sealed record InboundWhatsAppMessage(
    string From,
    string Id,
    string Content,
    string? ButtonPayload,
    string? ContextMessageId);

// Extrae el mensaje entrante del payload crudo de WhatsApp Cloud API. Antes solo se procesaban
// mensajes Type == "text"; los taps de botón (Type == "button"/"interactive") se descartaban en
// silencio porque WhatsAppWebhookMessage no tenía esas propiedades. Centralizado acá, aparte,
// porque es el código de mayor riesgo que se agrega y así queda testeable con un payload real
// de Meta sin necesitar un WebApplicationFactory.
public static class WhatsAppInboundExtractor
{
    private const string FallbackButtonLabel = "[boton]";

    public static InboundWhatsAppMessage? TryExtract(WhatsAppWebhookMessage message)
    {
        var contextMessageId = message.Context?.Id;

        return message.Type switch
        {
            "text" when message.Text is not null =>
                new InboundWhatsAppMessage(message.From, message.Id, message.Text.Body, null, contextMessageId),

            "button" when message.Button is not null =>
                new InboundWhatsAppMessage(
                    message.From,
                    message.Id,
                    message.Button.Text ?? message.Button.Payload ?? FallbackButtonLabel,
                    message.Button.Payload ?? message.Button.Text,
                    contextMessageId),

            "interactive" when message.Interactive?.ButtonReply is not null =>
                new InboundWhatsAppMessage(
                    message.From,
                    message.Id,
                    message.Interactive.ButtonReply.Title ?? message.Interactive.ButtonReply.Id ?? FallbackButtonLabel,
                    message.Interactive.ButtonReply.Id ?? message.Interactive.ButtonReply.Title,
                    contextMessageId),

            _ => null
        };
    }
}
