using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
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

    public string? HandoffTemplateName => options.Value.HandoffTemplateName;

    // El texto real aprobado por Meta para cada (WabaId, plantilla, idioma) no cambia entre
    // envíos, así que se cachea en proceso para no pegarle a la Graph API en cada mensaje.
    private static readonly ConcurrentDictionary<string, string?> TemplateBodyCache = new();

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
            var sentContent = await ResolveSentContentAsync(opts, request, ct);
            return new SendMessageResult(true, externalId, null, sentContent);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "[WhatsAppCloudApi] Error de red enviando a {Contact}", request.ToContact);
            return new SendMessageResult(false, null, ex.Message);
        }
    }

    // Quirk documentado de Meta para Argentina: nuestros números se guardan con el "9" móvil
    // (ej. 5491122602000, igual al wa_id real), pero el campo "to" de la Cloud API requiere
    // el número SIN ese "9" (5411122602000 -> 541122602000) o rechaza el envío / no matchea
    // la lista de números permitidos en modo desarrollo. Confirmado a mano contra la API real.
    public static string ToMetaWhatsAppFormat(string normalizedPhone)
    {
        return normalizedPhone.Length == 13 && normalizedPhone.StartsWith("549")
            ? "54" + normalizedPhone[3..]
            : normalizedPhone;
    }

    private static object BuildPayload(WhatsAppCloudApiOptions opts, string to, SendMessageRequest request)
    {
        var selection = ResolveTemplateSelection(opts, request);
        if (selection is not null)
        {
            var (name, language, bodyParameters) = selection.Value;

            // La plantilla de handoff (TemplateNameOverride) es una notificación interna, no
            // lleva los botones quick-reply de la plantilla de campaña.
            var quickReplyPayloads = string.IsNullOrWhiteSpace(request.TemplateNameOverride)
                ? opts.TemplateQuickReplyPayloads
                : [];

            return BuildTemplatePayload(to, name, language, bodyParameters, quickReplyPayloads);
        }

        // Sin plantilla aprobada por Meta configurada (o PreferFreeText): solo funciona dentro
        // de la ventana de servicio de 24hs (ej. respuestas a un inbound), no para contacto en frío.
        return new
        {
            messaging_product = "whatsapp",
            to,
            type = "text",
            text = new { body = request.Content }
        };
    }

    // Decide qué plantilla (nombre + idioma + parámetros de body) corresponde a este envío, o
    // null si va como texto libre. Compartido entre BuildPayload (arma el request a Meta) y
    // ResolveSentContentAsync (reconstruye el texto real para loguear en Message.Content).
    private static (string Name, string Language, IReadOnlyList<string>? BodyParameters)? ResolveTemplateSelection(
        WhatsAppCloudApiOptions opts, SendMessageRequest request)
    {
        // Plantilla ad-hoc (ej. handoff a vendedores) con parámetros explícitos: pisa
        // TemplateName/PreferFreeText.
        if (!string.IsNullOrWhiteSpace(request.TemplateNameOverride))
            return (request.TemplateNameOverride, opts.HandoffTemplateLanguage, request.TemplateParameters);

        // PreferFreeText fuerza texto libre aunque haya TemplateName configurada: usado para
        // respuestas dentro de la ventana de servicio de 24hs (catálogo, handoff sin plantilla
        // propia), donde texto libre es legal y evita repetir la plantilla de campaña.
        if (!request.PreferFreeText && !string.IsNullOrWhiteSpace(opts.TemplateName))
            return (opts.TemplateName, opts.TemplateLanguage, BuildTemplateBodyParameters(opts, request));

        return null;
    }

    // Reconstruye lo que el destinatario recibió de verdad: si se mandó texto libre, es
    // request.Content tal cual; si se mandó plantilla, hay que traer el body real aprobado por
    // Meta (nosotros solo tenemos el nombre, no el copy) y reemplazar sus {{n}} con los
    // parámetros usados. Devuelve null cuando no se pudo resolver (sin WabaId configurado, fetch
    // fallido, etc.) para que el caller caiga de vuelta a su propio texto local.
    private async Task<string?> ResolveSentContentAsync(WhatsAppCloudApiOptions opts, SendMessageRequest request, CancellationToken ct)
    {
        var selection = ResolveTemplateSelection(opts, request);
        if (selection is null)
            return request.Content;

        var (name, language, bodyParameters) = selection.Value;
        var bodyText = await GetTemplateBodyTextAsync(opts, name, language, ct);
        return bodyText is null ? null : SubstituteBodyParameters(bodyText, bodyParameters);
    }

    private async Task<string?> GetTemplateBodyTextAsync(WhatsAppCloudApiOptions opts, string templateName, string language, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(opts.WabaId))
            return null;

        var cacheKey = $"{opts.WabaId}:{templateName}:{language}";
        if (TemplateBodyCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            using var response = await httpClient.GetAsync(
                $"{opts.ApiVersion}/{opts.WabaId}/message_templates?name={Uri.EscapeDataString(templateName)}&fields=name,language,components",
                ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<WhatsAppTemplateListResponse>(body);
            var match = parsed?.Data?.FirstOrDefault(t => string.Equals(t.Language, language, StringComparison.OrdinalIgnoreCase));
            var bodyComponent = match?.Components?.FirstOrDefault(c => string.Equals(c.Type, "BODY", StringComparison.OrdinalIgnoreCase));

            TemplateBodyCache[cacheKey] = bodyComponent?.Text;
            return bodyComponent?.Text;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(
                ex, "[WhatsAppCloudApi] No se pudo obtener el texto real de la plantilla {Template}/{Language}.", templateName, language);
            return null;
        }
    }

    private static string SubstituteBodyParameters(string bodyText, IReadOnlyList<string>? parameters)
    {
        if (parameters is null or { Count: 0 })
            return bodyText;

        return Regex.Replace(bodyText, @"\{\{(\d+)\}\}", m =>
        {
            var index = int.Parse(m.Groups[1].Value) - 1;
            return index >= 0 && index < parameters.Count ? parameters[index] : m.Value;
        });
    }

    private static object BuildTemplatePayload(
        string to, string templateName, string language, IReadOnlyList<string>? bodyParameters, IList<string> quickReplyPayloads)
    {
        var components = new List<object>();

        if (bodyParameters is not null)
        {
            components.Add(new
            {
                type = "body",
                parameters = bodyParameters.Select(p => new { type = "text", text = p }).ToArray()
            });
        }

        // Meta rechaza el envío con (#132000) si el orden/cantidad de botones no coincide
        // EXACTO con lo aprobado para la plantilla, de ahí que el index se derive de la
        // posición configurada en TemplateQuickReplyPayloads (0, 1, ...).
        for (var i = 0; i < quickReplyPayloads.Count; i++)
        {
            components.Add(new
            {
                type = "button",
                sub_type = "quick_reply",
                index = i.ToString(),
                parameters = new object[] { new { type = "payload", payload = quickReplyPayloads[i] } }
            });
        }

        return new
        {
            messaging_product = "whatsapp",
            to,
            type = "template",
            template = components.Count == 0
                ? new { name = templateName, language = new { code = language } }
                : (object)new { name = templateName, language = new { code = language }, components = components.ToArray() }
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

    private class WhatsAppTemplateListResponse
    {
        [JsonPropertyName("data")]
        public List<WhatsAppTemplateData>? Data { get; set; }
    }

    private class WhatsAppTemplateData
    {
        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("components")]
        public List<WhatsAppTemplateComponent>? Components { get; set; }
    }

    private class WhatsAppTemplateComponent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
