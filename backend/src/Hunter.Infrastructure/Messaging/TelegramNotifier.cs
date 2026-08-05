using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Hunter.Application.Crm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Messaging;

// Cliente del Bot API de Telegram para alertas internas de leads (doc 23, sección 37 ampliada).
// El chat_id de cada usuario se carga a mano (ver ITelegramNotifier): Telegram no deja mandarle
// un DM a alguien por username, solo a un chat_id numérico que el bot recién conoce después de
// que esa persona le escribió una vez (ver @userinfobot en la guía de setup).
// https://core.telegram.org/bots/api#sendmessage
public class TelegramNotifier(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramNotifier> logger) : ITelegramNotifier
{
    public string? BotUsername => options.Value.BotUsername;

    public async Task<TelegramSendResult> SendAsync(string chatId, string message, CancellationToken ct = default)
    {
        try
        {
            // Un BotToken real tiene la forma "<id>:<secreto>" (con ":"). Combinar ese path como
            // relativo contra HttpClient.BaseAddress es frágil con cualquier estrategia de Uri:
            // pasado como string suelto, "bot12345:..." se lee como si "bot12345" fuera el
            // esquema (NotSupportedException); forzando UriKind.Relative, Uri sigue detectando
            // que "parece" absoluto y tira UriFormatException en el sentido contrario. La única
            // forma robusta es armar el string absoluto completo a mano: con el "https://" ya
            // puesto al principio, el primer "://" es inequívocamente el separador de esquema y
            // los ":" del token quedan adentro del path, donde son un carácter válido más.
            var requestUri = $"{httpClient.BaseAddress}bot{options.Value.BotToken}/sendMessage";
            using var response = await httpClient.PostAsJsonAsync(requestUri, new { chat_id = chatId, text = message }, ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(body);
                logger.LogWarning("[Telegram] Envío fallido a chat_id {ChatId}: {Status} {Error}", chatId, response.StatusCode, error ?? body);
                return new TelegramSendResult(false, error ?? $"HTTP {(int)response.StatusCode}");
            }

            return new TelegramSendResult(true, null);
        }
        catch (Exception ex)
        {
            // Se ensancha más allá de HttpRequestException/TaskCanceledException a propósito:
            // este método es un notificador de mejor esfuerzo (ver doc en InboundMessageService),
            // nunca debería poder tirar y romper a un caller que no siempre tiene su propio
            // try/catch (ver WebhooksController.TelegramInbound).
            logger.LogWarning(ex, "[Telegram] Error inesperado enviando a chat_id {ChatId}", chatId);
            return new TelegramSendResult(false, ex.Message);
        }
    }

    private static string? TryParseError(string body)
    {
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<TelegramErrorResponse>(body);
            return parsed?.Description;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private class TelegramErrorResponse
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
