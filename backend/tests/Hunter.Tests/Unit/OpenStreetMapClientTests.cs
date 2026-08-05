using System.Net;
using System.Text;
using System.Web;
using Hunter.Application.Prospecting;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Prospecting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class OpenStreetMapClientTests
{
    private static readonly IReadOnlyCollection<ProspectCategory> AllSupportedCategories = OpenStreetMapCategories.Supported;

    private class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }
        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private class FakeNominatimClient(Func<string, (double Lat, double Lon)?> resolver) : INominatimClient
    {
        public List<string> Queries { get; } = [];

        public Task<(double Lat, double Lon)?> GeocodeAsync(string query, CancellationToken ct = default)
        {
            Queries.Add(query);
            return Task.FromResult(resolver(query));
        }
    }

    private static (OpenStreetMapClient Client, FakeHttpMessageHandler Handler, FakeNominatimClient Nominatim) CreateClient(
        HttpStatusCode statusCode, string responseBody, Func<string, (double Lat, double Lon)?>? geocode = null)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new OpenStreetMapOptions());
        var nominatim = new FakeNominatimClient(geocode ?? (_ => (-34.6, -58.7)));
        return (new OpenStreetMapClient(httpClient, nominatim, options, NullLogger<OpenStreetMapClient>.Instance), handler, nominatim);
    }

    [Fact]
    public async Task SearchAsync_AreaMode_MapsTagsToCategoryAndAddress()
    {
        const string responseBody = """
        {
          "elements": [
            {
              "type": "node",
              "id": 123456,
              "tags": {
                "name": "Repuestos Oeste",
                "shop": "car_parts",
                "phone": "+54 11 1512-3456",
                "addr:street": "Av. Siempreviva",
                "addr:housenumber": "742",
                "addr:city": "Moreno",
                "addr:state": "Buenos Aires"
              }
            }
          ]
        }
        """;
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Single(results);
        var place = results[0];
        Assert.Equal("node/123456", place.ElementId);
        Assert.Equal("Repuestos Oeste", place.Name);
        Assert.Equal("Av. Siempreviva 742", place.Address);
        Assert.Equal("Moreno", place.City);
        Assert.Equal("Buenos Aires", place.Province);
        Assert.Equal("+54 11 1512-3456", place.PhoneNumber);
        Assert.Equal(ProspectCategory.AutoPartsStore, place.Category);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("area[\"name\"=\"Moreno\"]", HttpUtility.UrlDecode(handler.LastRequestBody));
    }

    [Fact]
    public async Task SearchAsync_AreaMode_MultipleLocalities_BuildsOneZonePerLocality()
    {
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""");

        await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno", "Merlo"], AllSupportedCategories, null, 50));

        var decoded = HttpUtility.UrlDecode(handler.LastRequestBody);
        Assert.Contains("area[\"name\"=\"Moreno\"]", decoded);
        Assert.Contains("area[\"name\"=\"Merlo\"]", decoded);
        Assert.DoesNotContain("around:", decoded);
    }

    [Fact]
    public async Task SearchAsync_RadiusMode_GeocodesLocalityAndBuildsAroundQuery()
    {
        var (client, handler, nominatim) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""", _ => (-34.65, -58.72));

        await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], [ProspectCategory.Workshop], 10, 50));

        Assert.Single(nominatim.Queries);
        Assert.Contains("Moreno", nominatim.Queries[0]);

        var decoded = HttpUtility.UrlDecode(handler.LastRequestBody);
        Assert.Contains("around:10000,-34.65,-58.72", decoded);
        Assert.DoesNotContain("area[\"name\"", decoded);
    }

    [Fact]
    public async Task SearchAsync_RadiusMode_LocalityThatFailsToGeocode_IsSkippedNotFatal()
    {
        var (client, handler, nominatim) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""",
            query => query.StartsWith("Moreno") ? (-34.65, -58.72) : null);

        await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno", "LocalidadInexistente"], [ProspectCategory.Workshop], 10, 50));

        Assert.Equal(2, nominatim.Queries.Count);
        var decoded = HttpUtility.UrlDecode(handler.LastRequestBody);
        Assert.Contains("around:10000,-34.65,-58.72", decoded);
        // Un solo rubro (Workshop) x una sola localidad geocodificada = una sola cláusula
        // "around:"; la localidad que no geocodificó no debe generar ninguna.
        Assert.Equal(2, decoded!.Split("around:").Length);
    }

    [Fact]
    public async Task SearchAsync_RadiusMode_AllLocalitiesFailToGeocode_ReturnsEmptyWithoutCallingOverpass()
    {
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""", _ => null);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["LocalidadInexistente"], [ProspectCategory.Workshop], 10, 50));

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData("car_repair", ProspectCategory.Workshop)]
    [InlineData("tyres", ProspectCategory.TireShop)]
    [InlineData("car", ProspectCategory.Reseller)]
    [InlineData("kiosco", ProspectCategory.Unknown)]
    public async Task SearchAsync_MapsShopTagToExpectedCategory(string shopTag, ProspectCategory expected)
    {
        var responseBody = $$"""
        {
          "elements": [
            { "type": "node", "id": 1, "tags": { "name": "Negocio", "shop": "{{shopTag}}", "phone": "123456" } }
          ]
        }
        """;
        var (client, _, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Single(results);
        Assert.Equal(expected, results[0].Category);
    }

    [Fact]
    public async Task SearchAsync_OilChangeServiceTag_TakesPrecedenceOverShopTag()
    {
        const string responseBody = """
        {
          "elements": [
            {
              "type": "node",
              "id": 1,
              "tags": {
                "name": "Lubricentro Norte",
                "shop": "car_repair",
                "service:vehicle:oil_change": "yes",
                "phone": "123456"
              }
            }
          ]
        }
        """;
        var (client, _, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Single(results);
        Assert.Equal(ProspectCategory.Lubricentro, results[0].Category);
    }

    [Fact]
    public async Task SearchAsync_ElementWithoutPhone_IsSkipped()
    {
        const string responseBody = """
        {
          "elements": [
            { "type": "node", "id": 1, "tags": { "name": "Sin teléfono", "shop": "car_parts" } }
          ]
        }
        """;
        var (client, _, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_DuplicateElements_AreDeduplicatedByElementId()
    {
        const string responseBody = """
        {
          "elements": [
            { "type": "node", "id": 1, "tags": { "name": "Negocio", "shop": "car_parts", "phone": "123456" } },
            { "type": "node", "id": 1, "tags": { "name": "Negocio", "shop": "car_parts", "phone": "123456" } }
          ]
        }
        """;
        var (client, _, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno", "Merlo"], AllSupportedCategories, null, 50));

        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ApiError_ReturnsEmptyList()
    {
        var (client, _, _) = CreateClient(HttpStatusCode.Forbidden, """{"error":"denied"}""");

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RateLimited_ReturnsEmptyListWithoutThrowing()
    {
        var (client, _, _) = CreateClient(HttpStatusCode.TooManyRequests, "");

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_NoElements_ReturnsEmptyList()
    {
        var (client, _, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""");

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], AllSupportedCategories, null, 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_NoLocalities_ReturnsEmptyWithoutCallingOverpass()
    {
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""");

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria([], AllSupportedCategories, null, 50));

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_NoCategories_ReturnsEmptyWithoutCallingOverpass()
    {
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""");

        var results = await client.SearchAsync(new OpenStreetMapSearchCriteria(["Moreno"], [], null, 50));

        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SearchAsync_LocalityWithQuotes_EscapesBeforeSendingQuery()
    {
        var (client, handler, _) = CreateClient(HttpStatusCode.OK, """{"elements":[]}""");

        const string cityWithQuotes = "San \"Mártir\" del Sur";
        await client.SearchAsync(new OpenStreetMapSearchCriteria([cityWithQuotes], AllSupportedCategories, null, 50));

        Assert.NotNull(handler.LastRequestBody);
        var decoded = HttpUtility.UrlDecode(handler.LastRequestBody);
        Assert.Contains("San \\\"Mártir\\\" del Sur", decoded);
        // La query no debe quedar rota: la comilla sin escapar no debe cerrar el literal de forma anticipada.
        Assert.DoesNotContain("area[\"name\"=\"San \"Mártir\"", decoded);
    }
}
