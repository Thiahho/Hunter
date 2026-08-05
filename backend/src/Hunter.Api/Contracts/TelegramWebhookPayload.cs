using System.Text.Json.Serialization;

namespace Hunter.Api.Contracts;

// Forma mínima del Update que manda Telegram al webhook (solo lo que necesitamos para el flujo
// de vinculación /start <code>). Referencia: https://core.telegram.org/bots/api#update
public class TelegramUpdate
{
    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; set; }
}

public class TelegramMessage
{
    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; } = null!;

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}
