using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hunter.Application.Campaigning;
using Hunter.Domain.Campaigning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Messaging;

// Envía mensajes vía WhatsApp Cloud API (Meta oficial, doc 12 sección 27 - decisión cerrada).
// Requiere WhatsAppCloudApi:PhoneNumberId y :AccessToken configurados; si no lo están,
// DependencyInjection registra StubMessageProvider en su lugar (ver AddInfrastructure).
public class WhatsAppCloudApiMessageProvider(
    HttpClient httpClient,
    IOptions<WhatsAppCloudApiOptions> options,
    ILogger<WhatsAppCloudApiMessageProvider> logger) : IMessageProvider
{
    public string ProviderName => "whatsapp_cloud_api";

    public async Task<SendMessageResult> SendAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        if (request.Channel != MessagingChannel.Whatsapp)
            return new SendMessageResult(false, null, $"WhatsAppCloudApiMessageProvider no soporta el canal {request.Channel}.");

        var opts = options.Value;
        var payload = BuildPayload(opts, request.ToContact, request.Content);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"{opts.ApiVersion}/{opts.PhoneNumberId}/messages", payload, ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(body);
                logger.LogWarning(
                    "[WhatsAppCloudApi] Envío fallido a {Contact}: {Status} {Error}",
                    request.ToContact, response.StatusCode, error ?? body);
                return new SendMessageResult(false, null, error ?? $"HTTP {(int)response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<WhatsAppSendResponse>(body);
            var externalId = result?.Messages?.FirstOrDefault()?.Id;
            return new SendMessageResult(true, externalId, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "[WhatsAppCloudApi] Error de red enviando a {Contact}", request.ToContact);
            return new SendMessageResult(false, null, ex.Message);
        }
    }

    private static object BuildPayload(WhatsAppCloudApiOptions opts, string to, string content)
    {
        if (!string.IsNullOrWhiteSpace(opts.TemplateName))
        {
            return new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template = new
                {
                    name = opts.TemplateName,
                    language = new { code = opts.TemplateLanguage },
                    components = new object[]
                    {
                        new
                        {
                            type = "body",
                            parameters = new object[] { new { type = "text", text = content } }
                        }
                    }
                }
            };
        }

        // Sin plantilla aprobada por Meta configurada: solo funciona dentro de la ventana
        // de servicio de 24hs (ej. respuestas a un inbound), no para contacto en frío.
        return new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = content }
        };
    }

    private static string? TryParseError(string body)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<WhatsAppErrorResponse>(body);
            return parsed?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private class WhatsAppSendResponse
    {
        [JsonPropertyName("messages")]
        public List<WhatsAppSendMessageId>? Messages { get; set; }
    }

    private class WhatsAppSendMessageId
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private class WhatsAppErrorResponse
    {
        [JsonPropertyName("error")]
        public WhatsAppError? Error { get; set; }
    }

    private class WhatsAppError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
