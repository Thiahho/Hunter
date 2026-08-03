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
        var toContact = ToMetaWhatsAppFormat(request.ToContact);
        var payload = BuildPayload(opts, toContact, request);

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

    // Quirk documentado de Meta para Argentina: nuestros números se guardan con el "9" móvil
    // (ej. 5491122692061, igual al wa_id real), pero el campo "to" de la Cloud API requiere
    // el número SIN ese "9" (5411122692061 -> 541122692061) o rechaza el envío / no matchea
    // la lista de números permitidos en modo desarrollo. Confirmado a mano contra la API real.
    public static string ToMetaWhatsAppFormat(string normalizedPhone)
    {
        return normalizedPhone.Length == 13 && normalizedPhone.StartsWith("549")
            ? "54" + normalizedPhone[3..]
            : normalizedPhone;
    }

    private static object BuildPayload(WhatsAppCloudApiOptions opts, string to, SendMessageRequest request)
    {
        if (!string.IsNullOrWhiteSpace(opts.TemplateName))
        {
            var bodyParameters = BuildTemplateBodyParameters(opts, request);

            return new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template = bodyParameters is null
                    ? new
                    {
                        name = opts.TemplateName,
                        language = new { code = opts.TemplateLanguage }
                    }
                    : (object)new
                    {
                        name = opts.TemplateName,
                        language = new { code = opts.TemplateLanguage },
                        components = new object[]
                        {
                            new
                            {
                                type = "body",
                                parameters = bodyParameters.Select(p => new { type = "text", text = p }).ToArray()
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
            text = new { body = request.Content }
        };
    }

    // Meta rechaza el envío con (#132000) si la cantidad de parámetros no coincide EXACTO
    // con lo aprobado para la plantilla, de ahí la necesidad de configurarla por plantilla.
    private static string[]? BuildTemplateBodyParameters(WhatsAppCloudApiOptions opts, SendMessageRequest request) =>
        opts.TemplateBodyParameterCount switch
        {
            0 => null,
            1 => new[] { request.RecipientName ?? request.Content },
            2 => new[] { request.RecipientName ?? request.Content, opts.TemplateSecondParameter ?? string.Empty },
            _ => throw new InvalidOperationException(
                $"WhatsAppCloudApi:TemplateBodyParameterCount = {opts.TemplateBodyParameterCount} no soportado (valores válidos: 0, 1 o 2).")
        };

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
