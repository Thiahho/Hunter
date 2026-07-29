using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Hunter.Application.Prospecting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.Prospecting;

// Cliente de Places API (New) de Google (doc 21, sección 8 - primera fuente externa).
// https://developers.google.com/maps/documentation/places/web-service/text-search
public class GooglePlacesClient(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options,
    ILogger<GooglePlacesClient> logger) : IGooglePlacesClient
{
    private const string FieldMask = "places.id,places.displayName,places.formattedAddress,places.nationalPhoneNumber,places.addressComponents";

    public async Task<IReadOnlyList<GooglePlaceResult>> SearchTextAsync(string query, int maxResults, CancellationToken ct = default)
    {
        var clampedMax = Math.Clamp(maxResults, 1, 20);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/places:searchText")
        {
            Content = JsonContent.Create(new
            {
                textQuery = query,
                languageCode = "es",
                maxResultCount = clampedMax
            })
        };
        httpRequest.Headers.Add("X-Goog-Api-Key", options.Value.ApiKey);
        httpRequest.Headers.Add("X-Goog-FieldMask", FieldMask);

        using var response = await httpClient.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("[GooglePlaces] Búsqueda fallida para \"{Query}\": {Status} {Body}", query, response.StatusCode, body);
            return [];
        }

        var parsed = System.Text.Json.JsonSerializer.Deserialize<SearchTextResponse>(body);
        if (parsed?.Places is null)
            return [];

        return parsed.Places.Select(MapPlace).ToList();
    }

    private static GooglePlaceResult MapPlace(Place place)
    {
        var city = place.AddressComponents?.FirstOrDefault(c => c.Types.Contains("locality"))?.LongText;
        var province = place.AddressComponents?.FirstOrDefault(c => c.Types.Contains("administrative_area_level_1"))?.LongText;

        return new GooglePlaceResult(
            place.Id,
            place.DisplayName?.Text ?? "Sin nombre",
            place.FormattedAddress,
            city,
            province,
            place.NationalPhoneNumber);
    }

    private class SearchTextResponse
    {
        [JsonPropertyName("places")]
        public List<Place>? Places { get; set; }
    }

    private class Place
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("displayName")]
        public DisplayName? DisplayName { get; set; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("nationalPhoneNumber")]
        public string? NationalPhoneNumber { get; set; }

        [JsonPropertyName("addressComponents")]
        public List<AddressComponent>? AddressComponents { get; set; }
    }

    private class DisplayName
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class AddressComponent
    {
        [JsonPropertyName("longText")]
        public string? LongText { get; set; }

        [JsonPropertyName("types")]
        public List<string> Types { get; set; } = [];
    }
}
