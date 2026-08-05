using System.Globalization;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Prospecting;

// Cliente de Nominatim, el geocoder gratuito del ecosistema OSM (necesario para resolver el
// lat/lon de una localidad cuando OpenStreetMapClient arma una búsqueda por radio).
// https://nominatim.org/release-docs/latest/api/Search/
// El throttling de 1 request/segundo que exige la política de uso de Nominatim es
// responsabilidad del LLAMADOR (OpenStreetMapClient itera localidades secuencialmente con
// delay): este cliente solo resuelve una consulta por llamada.
public class NominatimClient(
    HttpClient httpClient,
    IOptions<NominatimOptions> options,
    ILogger<NominatimClient> logger) : INominatimClient
{
    public async Task<(double Lat, double Lon)?> GeocodeAsync(string query, CancellationToken ct = default)
    {
        var opts = options.Value;
        var url = $"{opts.Endpoint}?q={HttpUtility.UrlEncode(query)}&format=jsonv2&limit=1&countrycodes=ar";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(opts.UserAgent);

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("[Nominatim] Geocoding fallido para \"{Query}\": {Status}", query, response.StatusCode);
            return null;
        }

        List<NominatimResult>? results;
        try
        {
            results = System.Text.Json.JsonSerializer.Deserialize<List<NominatimResult>>(body);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "[Nominatim] Respuesta no parseable para \"{Query}\".", query);
            return null;
        }

        var first = results?.FirstOrDefault();
        if (first is null ||
            !double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
        {
            logger.LogWarning("[Nominatim] Sin resultados para \"{Query}\".", query);
            return null;
        }

        return (lat, lon);
    }

    private class NominatimResult
    {
        [JsonPropertyName("lat")]
        public string? Lat { get; set; }

        [JsonPropertyName("lon")]
        public string? Lon { get; set; }
    }
}
