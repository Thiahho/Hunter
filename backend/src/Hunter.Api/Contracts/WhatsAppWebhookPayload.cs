using System.Text.Json.Serialization;

namespace Hunter.Api.Contracts;

// Forma real del payload que envía Meta al webhook de WhatsApp Cloud API.
// Referencia: https://developers.facebook.com/docs/whatsapp/cloud-api/webhooks/payload-examples
public class WhatsAppWebhookPayload
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("entry")]
    public List<WhatsAppWebhookEntry> Entry { get; set; } = [];
}

public class WhatsAppWebhookEntry
{
    [JsonPropertyName("changes")]
    public List<WhatsAppWebhookChange> Changes { get; set; } = [];
}

public class WhatsAppWebhookChange
{
    [JsonPropertyName("value")]
    public WhatsAppWebhookValue? Value { get; set; }
}

public class WhatsAppWebhookValue
{
    [JsonPropertyName("messages")]
    public List<WhatsAppWebhookMessage>? Messages { get; set; }

    [JsonPropertyName("statuses")]
    public List<WhatsAppWebhookStatus>? Statuses { get; set; }
}

public class WhatsAppWebhookStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = null!;

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("errors")]
    public List<WhatsAppWebhookStatusError>? Errors { get; set; }
}

public class WhatsAppWebhookStatusError
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class WhatsAppWebhookMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = null!;

    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("text")]
    public WhatsAppWebhookText? Text { get; set; }

    // Tap de un botón quick_reply de una plantilla (ej. "Soy Mayorista" / "Tengo Casa de
    // Repuestos"). Meta manda estos con Type == "button".
    [JsonPropertyName("button")]
    public WhatsAppWebhookButton? Button { get; set; }

    // Tap de un botón de un mensaje interactivo (no de plantilla). Meta manda estos con
    // Type == "interactive". No lo usamos hoy, pero lo parseamos para no volver a caer en el
    // mismo bug de descarte silencioso si algún día se manda un mensaje interactivo.
    [JsonPropertyName("interactive")]
    public WhatsAppWebhookInteractive? Interactive { get; set; }

    // Referencia al mensaje saliente que originó esta respuesta (id = wamid). Permite
    // correlacionar la respuesta con la campaña/plantilla exacta que se envió, incluso para
    // mensajes de texto plano.
    [JsonPropertyName("context")]
    public WhatsAppWebhookContext? Context { get; set; }
}

public class WhatsAppWebhookText
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = null!;
}

public class WhatsAppWebhookButton
{
    // Valor exacto configurado al enviar el componente "button" (sub_type: quick_reply).
    // Es lo que usamos para mapear al rubro (QuickReplyButtonMapper). Meta puede omitirlo o
    // repetir el texto acá si el envío no incluyó el parámetro de payload.
    [JsonPropertyName("payload")]
    public string? Payload { get; set; }

    // Etiqueta visible del botón (ej. "Soy Mayorista").
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class WhatsAppWebhookInteractive
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("button_reply")]
    public WhatsAppWebhookButtonReply? ButtonReply { get; set; }
}

public class WhatsAppWebhookButtonReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class WhatsAppWebhookContext
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }
}
