using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hunter.Application.Prospecting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Prospecting;

// Cliente del actor de Apify "compass/crawler-google-places" (Google Maps Scraper): a diferencia
// de OpenStreetMapClient, acá "rubro" es siempre texto libre buscado tal cual en Google Maps (sin
// mapeo a un tag cerrado), así que cubre cualquier rubro que el usuario escriba. Es un servicio
// pago (~USD 1.50 cada 1000 lugares), por eso los límites de resultados acá son más chicos que en
// OpenStreetMap/Nominatim (gratis).
// https://apify.com/compass/crawler-google-places
public class ApifyGoogleMapsClient(
    HttpClient httpClient,
    IOptions<ApifyOptions> options,
    ILogger<ApifyGoogleMapsClient> logger) : IApifyGoogleMapsClient
{
    // Tope defensivo de costo: con N localidades x M rubros se arma una search string por
    // combinación, así que el resultado total pedido se reparte entre todas esas búsquedas en vez
    // de multiplicarse por cada una (ver PerSearchCap más abajo).
    private const int MaxResultsCap = 100;

    public async Task<IReadOnlyList<ApifyPlaceResult>> SearchAsync(ApifySearchCriteria criteria, CancellationToken ct = default)
    {
        var localities = criteria.Localities.Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList();
        var keywords = criteria.Keywords.Select(k => k.Trim()).Where(k => k.Length > 0).Distinct().ToList();
        if (localities.Count == 0 || keywords.Count == 0)
            return [];

        var searchStrings = new List<string>();
        foreach (var locality in localities)
            foreach (var keyword in keywords)
                searchStrings.Add($"{keyword} en {locality}, Argentina");

        var clampedMax = Math.Clamp(criteria.MaxResults, 1, MaxResultsCap);
        var perSearchCap = Math.Clamp((int)Math.Ceiling(clampedMax / (double)searchStrings.Count), 1, clampedMax);

        var logLabel = $"{string.Join(", ", keywords)} / {string.Join(", ", localities)}";

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post, $"v2/actors/{options.Value.ActorId}/run-sync-get-dataset-items?timeout={options.Value.TimeoutSeconds}")
        {
            Content = JsonContent.Create(new
            {
                searchStringsArray = searchStrings,
                maxCrawledPlacesPerSearch = perSearchCap,
                language = "es"
            })
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.ApiToken);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("[Apify] Búsqueda fallida para \"{Search}\": {Status} {Body}", logLabel, response.StatusCode, body);
            return [];
        }

        List<ApifyDatasetItem>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<ApifyDatasetItem>>(body);
        }
        catch (JsonException ex)
        {
            // Si el run no terminó dentro de los 300s duros del endpoint sync, Apify responde con
            // el objeto del run (status Running) en vez de un array de items — cae acá en vez de
            // silenciarse como "sin resultados" sin explicación.
            logger.LogWarning(ex, "[Apify] Respuesta no parseable para \"{Search}\" (¿el run no terminó a tiempo?).", logLabel);
            return [];
        }

        if (items is null)
            return [];

        return items
            .Select(MapItem)
            .Where(r => r is not null)
            .Select(r => r!)
            .DistinctBy(r => r.PlaceId)
            .Take(clampedMax)
            .ToList();
    }

    private static ApifyPlaceResult? MapItem(ApifyDatasetItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            return null;

        return new ApifyPlaceResult(
            item.PlaceId ?? item.Title,
            item.Title,
            item.Address,
            item.City,
            item.State,
            item.Phone ?? item.PhoneUnformatted);
    }

    private class ApifyDatasetItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("placeId")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("phoneUnformatted")]
        public string? PhoneUnformatted { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
}
