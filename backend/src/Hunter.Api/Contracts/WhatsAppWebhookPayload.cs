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
}

public class WhatsAppWebhookText
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = null!;
}
