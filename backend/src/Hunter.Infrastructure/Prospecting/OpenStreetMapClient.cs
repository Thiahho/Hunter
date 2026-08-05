using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Hunter.Application.Prospecting;
using Hunter.Domain.Prospecting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Prospecting;

// Cliente de la Overpass API de OpenStreetMap (fuente complementaria a Google Places, doc 21
// sección 8 ampliada): a diferencia de Google, no cobra por teléfono ni tiene cuota mensual
// dura (~10.000 requests/día de margen), así que sirve para no depender de un único proveedor.
// https://wiki.openstreetmap.org/wiki/Overpass_API
public class OpenStreetMapClient(
    HttpClient httpClient,
    INominatimClient nominatimClient,
    IOptions<OpenStreetMapOptions> options,
    ILogger<OpenStreetMapClient> logger) : IOpenStreetMapClient
{
    // Nominatim exige máximo 1 request/segundo; se geocodifica una localidad a la vez con este
    // margen entre llamadas en vez de lanzarlas en paralelo.
    private const int NominatimThrottleMs = 1100;

    // 429 (rate limit) y 504 (timeout del servidor público bajo carga) son fallas transitorias
    // esperables en Overpass; se reintenta con backoff antes de darse por vencido. Otros errores
    // (4xx propios de la query, etc.) no son transitorios y fallan en el primer intento.
    private static readonly TimeSpan[] TransientRetryDelays = [TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(1)];

    public async Task<IReadOnlyList<OpenStreetMapPlaceResult>> SearchAsync(OpenStreetMapSearchCriteria criteria, CancellationToken ct = default)
    {
        var clampedMax = Math.Clamp(criteria.MaxResults, 1, 300);
        var localities = criteria.Localities.Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList();
        if (localities.Count == 0)
            return [];

        var categoryFilters = criteria.Categories.Select(CategoryToOsmFilter);
        var keywordFilters = (criteria.Keywords ?? []).Select(KeywordToOsmFilter);
        var filters = categoryFilters.Concat(keywordFilters).ToList();
        if (filters.Count == 0)
            return [];

        var logLabel = string.Join(", ", localities);
        var timeoutSeconds = options.Value.TimeoutSeconds;

        string query;
        if (criteria.RadiusKm is int radiusKm)
        {
            var centers = await GeocodeLocalitiesAsync(localities, ct);
            if (centers.Count == 0)
            {
                logger.LogWarning("[OpenStreetMap] Ninguna localidad pudo geocodificarse para \"{Localities}\".", logLabel);
                return [];
            }

            query = BuildRadiusQuery(centers, filters, radiusKm * 1000, timeoutSeconds, clampedMax);
        }
        else
        {
            query = BuildAreaQuery(localities, filters, timeoutSeconds, clampedMax);
        }

        var body = await PostWithRetryAsync(query, logLabel, ct);
        if (body is null)
            return [];

        OverpassResponse? parsed;
        try
        {
            parsed = System.Text.Json.JsonSerializer.Deserialize<OverpassResponse>(body);
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "[OpenStreetMap] Respuesta no parseable para \"{Localities}\".", logLabel);
            return [];
        }

        if (!string.IsNullOrEmpty(parsed?.Remark))
            logger.LogWarning("[OpenStreetMap] Overpass devolvió remark para \"{Localities}\": {Remark}", logLabel, parsed.Remark);

        if (parsed?.Elements is null)
            return [];

        // DistinctBy: con varias localidades/rubros en una sola query, un mismo negocio puede
        // matchear más de una cláusula (zonas superpuestas, o un comercio con dos rubros
        // tageados) y aparecer repetido en "elements".
        return parsed.Elements
            .Select(MapElement)
            .Where(r => r is not null)
            .Select(r => r!)
            .DistinctBy(r => r.ElementId)
            .Take(clampedMax)
            .ToList();
    }

    // Se postea contra la URL absoluta (no BaseAddress + ruta relativa, como en
    // GooglePlacesClient): el Endpoint de Overpass ya es la URL completa del interprete, sin un
    // esquema de versionado de rutas que amerite componerla en dos partes.
    // Devuelve null (ya logueado) si falla después de agotar los reintentos en fallas
    // transitorias, o de inmediato en fallas no transitorias.
    private async Task<string?> PostWithRetryAsync(string query, string logLabel, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query });
            using var response = await httpClient.PostAsync(options.Value.Endpoint, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
                return body;

            var isTransient = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.GatewayTimeout;
            if (isTransient && attempt < TransientRetryDelays.Length)
            {
                logger.LogWarning(
                    "[OpenStreetMap] {Status} para \"{Localities}\" (intento {Attempt}/{Max}), reintentando...",
                    response.StatusCode, logLabel, attempt + 1, TransientRetryDelays.Length + 1);
                await Task.Delay(TransientRetryDelays[attempt], ct);
                continue;
            }

            var reason = response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => "rate limit de Overpass alcanzado",
                HttpStatusCode.GatewayTimeout => "timeout del servidor de Overpass",
                _ => body
            };
            logger.LogWarning("[OpenStreetMap] Búsqueda fallida para \"{Localities}\": {Status} {Reason}", logLabel, response.StatusCode, reason);
            return null;
        }
    }

    private async Task<List<(string Locality, double Lat, double Lon)>> GeocodeLocalitiesAsync(List<string> localities, CancellationToken ct)
    {
        var centers = new List<(string, double, double)>();

        for (var i = 0; i < localities.Count; i++)
        {
            if (i > 0)
                await Task.Delay(NominatimThrottleMs, ct);

            var geocoded = await nominatimClient.GeocodeAsync($"{localities[i]}, Argentina", ct);
            if (geocoded is null)
            {
                logger.LogWarning("[OpenStreetMap] No se pudo geocodificar \"{Locality}\", se omite del radio de búsqueda.", localities[i]);
                continue;
            }

            centers.Add((localities[i], geocoded.Value.Lat, geocoded.Value.Lon));
        }

        return centers;
    }

    // Escapa comillas y backslashes antes de interpolar en la query: sin esto, un nombre de
    // localidad con comillas rompería la sintaxis de Overpass QL (o, en el peor caso, permitiría
    // inyectar cláusulas propias).
    private static string EscapeQl(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // Filtro de teléfono como par de claves exactas, no como regex sobre el nombre de la clave
    // (["~"^(contact:)?phone$"~"."]): esa forma obligaba a Overpass a escanear todos los tags de
    // cada elemento candidato en vez de usar el índice de la clave, y bajo carga pública eso solo
    // alcanza a evaluar una fracción del área antes de pisar el [timeout:N] (confirmado a mano:
    // la versión regex tardaba >15s y volvía con remark de timeout y 0 elementos para Moreno /
    // shop=car_repair, mientras que estas dos cláusulas exactas resuelven en ~2s). El dedup por
    // ElementId en SearchAsync ya cubre el caso de un elemento con ambos tags.
    private static readonly string[] PhoneKeyFilters = ["[\"phone\"]", "[\"contact:phone\"]"];

    // Único mapeo de ProspectCategory a un filtro de tag de OSM: Lubricentro no tiene un
    // shop=... propio en OSM, así que se modela como taller (car_repair) que además ofrece el
    // servicio específico de cambio de aceite.
    private static string CategoryToOsmFilter(ProspectCategory category) => category switch
    {
        ProspectCategory.AutoPartsStore => "[\"shop\"=\"car_parts\"]",
        ProspectCategory.Workshop => "[\"shop\"=\"car_repair\"]",
        ProspectCategory.TireShop => "[\"shop\"=\"tyres\"]",
        ProspectCategory.Reseller => "[\"shop\"=\"car\"]",
        ProspectCategory.Lubricentro => "[\"shop\"=\"car_repair\"][\"service:vehicle:oil_change\"=\"yes\"]",
        _ => throw new ArgumentOutOfRangeException(
            nameof(category), category, "Categoría no soportada por OpenStreetMap (ver OpenStreetMapCategories.Supported).")
    };

    // Búsqueda libre por rubro no mapeado a un tag conocido (ej. "peluquería"): en vez de un tag
    // exacto, matchea por coincidencia (case-insensitive) en el nombre del comercio. Regex.Escape
    // neutraliza metacaracteres de regex en lo que escribió el usuario (para que "Auto+Repuestos"
    // se busque literal y no falle o degenere en un patrón costoso para Overpass); EscapeQl encima
    // blinda la interpolación en el string de la query QL.
    private static string KeywordToOsmFilter(string keyword) =>
        $"[\"name\"~\"{EscapeQl(Regex.Escape(keyword))}\",i]";

    private static string BuildRadiusQuery(
        List<(string Locality, double Lat, double Lon)> centers, List<string> filters,
        int radiusMeters, int timeoutSeconds, int maxResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[out:json][timeout:{timeoutSeconds}];");
        sb.AppendLine("(");
        foreach (var (_, lat, lon) in centers)
        {
            var latStr = lat.ToString(CultureInfo.InvariantCulture);
            var lonStr = lon.ToString(CultureInfo.InvariantCulture);
            foreach (var filter in filters)
            {
                // phoneFilter primero: es una condición de presencia de clave (barata, muy
                // selectiva) y acota el conjunto de candidatos ANTES de evaluar filter. Importa
                // sobre todo para KeywordToOsmFilter (name~regex): un regex no usa índice, así
                // que evaluarlo contra "todo lo que tenga nombre" en el radio (calles, ríos, todo
                // POI) en vez de contra "lo que ya tiene teléfono" es lo que hacía pisar el
                // [timeout:N] y volver sin resultados en vez de encontrar el negocio buscado.
                foreach (var phoneFilter in PhoneKeyFilters)
                    sb.AppendLine($"  nwr(around:{radiusMeters},{latStr},{lonStr}){phoneFilter}{filter};");
            }
        }
        sb.AppendLine(");");
        sb.AppendLine($"out center {maxResults};");
        return sb.ToString();
    }

    private static string BuildAreaQuery(List<string> localities, List<string> filters, int timeoutSeconds, int maxResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[out:json][timeout:{timeoutSeconds}];");
        sb.AppendLine("area[\"ISO3166-1\"=\"AR\"][admin_level=2]->.ar;");

        for (var i = 0; i < localities.Count; i++)
            sb.AppendLine($"area[\"name\"=\"{EscapeQl(localities[i])}\"](area.ar)->.zona{i};");

        sb.AppendLine("(");
        for (var i = 0; i < localities.Count; i++)
        {
            foreach (var filter in filters)
            {
                // Mismo motivo que en BuildRadiusQuery: phoneFilter primero para acotar antes de
                // evaluar filter (crítico cuando filter es un name~regex sin índice).
                foreach (var phoneFilter in PhoneKeyFilters)
                    sb.AppendLine($"  nwr{phoneFilter}(area.zona{i}){filter};");
            }
        }
        sb.AppendLine(");");
        sb.AppendLine($"out center {maxResults};");
        return sb.ToString();
    }

    // Sin fallback de ciudad/provincia: a diferencia de la versión de una sola localidad, acá no
    // hay forma de saber a cuál de las N localidades buscadas pertenece un elemento sin
    // fallback, así que se usa lo que traiga el propio tag (o null si no lo tiene).
    private static OpenStreetMapPlaceResult? MapElement(OverpassElement element)
    {
        var tags = element.Tags;
        if (tags is null || !tags.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
            return null;

        var phone = tags.GetValueOrDefault("phone") ?? tags.GetValueOrDefault("contact:phone");
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        var street = tags.GetValueOrDefault("addr:street");
        var houseNumber = tags.GetValueOrDefault("addr:housenumber");
        var address = street is null ? null : houseNumber is null ? street : $"{street} {houseNumber}";

        return new OpenStreetMapPlaceResult(
            $"{element.Type}/{element.Id}",
            name,
            address,
            tags.GetValueOrDefault("addr:city"),
            tags.GetValueOrDefault("addr:state"),
            phone,
            MapCategory(tags));
    }

    // Se evalúa service:vehicle:oil_change primero: un lubricentro suele estar tageado como
    // shop=car_repair además del servicio específico, y ese matiz es justamente lo que Google
    // Places no puede darnos (siempre devuelve Category=Unknown).
    private static ProspectCategory MapCategory(Dictionary<string, string> tags)
    {
        if (tags.GetValueOrDefault("service:vehicle:oil_change") == "yes")
            return ProspectCategory.Lubricentro;

        return tags.GetValueOrDefault("shop") switch
        {
            "car_parts" => ProspectCategory.AutoPartsStore,
            "car_repair" => ProspectCategory.Workshop,
            "tyres" => ProspectCategory.TireShop,
            "car" => ProspectCategory.Reseller,
            _ => ProspectCategory.Unknown
        };
    }

    private class OverpassResponse
    {
        [JsonPropertyName("elements")]
        public List<OverpassElement>? Elements { get; set; }

        // Presente cuando Overpass corta la ejecución antes de terminar (ej. "Query timed out
        // in ... after N seconds"): igual devuelve 200 con los elementos ya encontrados hasta
        // ese punto (o ninguno), así que sin loguear esto un timeout se ve idéntico a "sin
        // resultados" en los logs.
        [JsonPropertyName("remark")]
        public string? Remark { get; set; }
    }

    private class OverpassElement
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "node";

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }
}
