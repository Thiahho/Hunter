using System.Net;
using System.Text;
using Hunter.Infrastructure.Prospecting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class GooglePlacesClientTests
{
    private class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    private static GooglePlacesClient CreateClient(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://places.googleapis.com/") };
        var options = Options.Create(new GooglePlacesOptions { ApiKey = "test-key" });
        return new GooglePlacesClient(httpClient, options, NullLogger<GooglePlacesClient>.Instance);
    }

    [Fact]
    public async Task SearchTextAsync_ValidResponse_MapsCityAndProvinceFromAddressComponents()
    {
        const string responseBody = """
        {
          "places": [
            {
              "id": "place123",
              "displayName": { "text": "Repuestos Oeste", "languageCode": "es" },
              "formattedAddress": "Av. Siempreviva 742, Moreno, Buenos Aires, Argentina",
              "nationalPhoneNumber": "11 1512-3456",
              "addressComponents": [
                { "longText": "742", "types": ["street_number"] },
                { "longText": "Moreno", "types": ["locality", "political"] },
                { "longText": "Buenos Aires", "types": ["administrative_area_level_1", "political"] },
                { "longText": "Argentina", "types": ["country", "political"] }
              ]
            }
          ]
        }
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        var results = await client.SearchTextAsync("repuestos de auto en Moreno", 5);

        Assert.Single(results);
        var place = results[0];
        Assert.Equal("place123", place.PlaceId);
        Assert.Equal("Repuestos Oeste", place.Name);
        Assert.Equal("Moreno", place.City);
        Assert.Equal("Buenos Aires", place.Province);
        Assert.Equal("11 1512-3456", place.PhoneNumber);

        Assert.Equal("test-key", handler.LastRequest!.Headers.GetValues("X-Goog-Api-Key").Single());
    }

    [Fact]
    public async Task SearchTextAsync_ApiError_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, """{"error":{"message":"denied"}}""");
        var client = CreateClient(handler);

        var results = await client.SearchTextAsync("query", 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchTextAsync_NoPlaces_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"places":[]}""");
        var client = CreateClient(handler);

        var results = await client.SearchTextAsync("query", 5);

        Assert.Empty(results);
    }
}
