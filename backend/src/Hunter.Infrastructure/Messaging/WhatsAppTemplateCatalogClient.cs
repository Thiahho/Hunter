using System.Text.Json;
using System.Text.Json.Serialization;
using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Messaging;

// Trae del Graph API de Meta las plantillas de WhatsApp aprobadas para la WABA configurada, para
// que la web deje elegir entre ellas en vez de que alguien tipee contenido a mano (el copy real
// solo existe donde Meta lo aprobó).
public class WhatsAppTemplateCatalogClient(
    HttpClient httpClient,
    IOptions<WhatsAppCloudApiOptions> options,
    ILogger<WhatsAppTemplateCatalogClient> logger) : IWhatsAppTemplateCatalogClient
{
    public async Task<Result<IReadOnlyList<MetaWhatsAppTemplateDto>>> ListApprovedAsync(CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.WabaId))
            return Result<IReadOnlyList<MetaWhatsAppTemplateDto>>.Failure(
                "Falta configurar WhatsAppCloudApi:WabaId para poder consultar las plantillas aprobadas en Meta.");

        try
        {
            using var response = await httpClient.GetAsync(
                $"{opts.ApiVersion}/{opts.WabaId}/message_templates?fields=name,language,status,components&limit=100",
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("[WhatsAppTemplateCatalog] Meta devolvió {Status}: {Body}", response.StatusCode, body);
                return Result<IReadOnlyList<MetaWhatsAppTemplateDto>>.Failure(
                    $"Meta devolvió un error al listar las plantillas (HTTP {(int)response.StatusCode}).");
            }

            var parsed = JsonSerializer.Deserialize<TemplateListResponse>(body);
            var templates = (parsed?.Data ?? [])
                .Where(t => t.Name is not null && t.Language is not null)
                .Select(t => new MetaWhatsAppTemplateDto(
                    t.Name!,
                    t.Language!,
                    t.Status ?? "UNKNOWN",
                    t.Components?.FirstOrDefault(c => string.Equals(c.Type, "BODY", StringComparison.OrdinalIgnoreCase))?.Text))
                .ToList();

            return Result<IReadOnlyList<MetaWhatsAppTemplateDto>>.Success(templates);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning(ex, "[WhatsAppTemplateCatalog] No se pudo listar las plantillas de Meta.");
            return Result<IReadOnlyList<MetaWhatsAppTemplateDto>>.Failure("No se pudo conectar con Meta para listar las plantillas.");
        }
    }

    private class TemplateListResponse
    {
        [JsonPropertyName("data")]
        public List<TemplateData>? Data { get; set; }
    }

    private class TemplateData
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("components")]
        public List<TemplateComponent>? Components { get; set; }
    }

    private class TemplateComponent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
