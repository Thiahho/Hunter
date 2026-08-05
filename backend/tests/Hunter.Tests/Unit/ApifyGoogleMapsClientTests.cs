using System.Net;
using System.Text;
using System.Text.Json;
using Hunter.Application.Prospecting;
using Hunter.Infrastructure.Prospecting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class ApifyGoogleMapsClientTests
{
    private class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private static ApifyGoogleMapsClient CreateClient(FakeHttpMessageHandler handler, string apiToken = "test-token")
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.apify.com/") };
        var options = Options.Create(new ApifyOptions { ApiToken = apiToken });
        return new ApifyGoogleMapsClient(httpClient, options, NullLogger<ApifyGoogleMapsClient>.Instance);
    }

    [Fact]
    public async Task SearchAsync_ValidResponse_MapsFieldsAndSendsBearerToken()
    {
        const string responseBody = """
        [
          {
            "title": "Peluquería Ana",
            "placeId": "ChIJabc123",
            "phone": "011 4444-5555",
            "phoneUnformatted": "+541144445555",
            "address": "Av. Siempreviva 742",
            "city": "Moreno",
            "state": "Buenos Aires"
          }
        ]
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler, "my-token");

        var results = await client.SearchAsync(new ApifySearchCriteria(["Peluquería"], ["Moreno"], 50));

        Assert.Single(results);
        var place = results[0];
        Assert.Equal("ChIJabc123", place.PlaceId);
        Assert.Equal("Peluquería Ana", place.Name);
        Assert.Equal("Av. Siempreviva 742", place.Address);
        Assert.Equal("Moreno", place.City);
        Assert.Equal("Buenos Aires", place.Province);
        Assert.Equal("011 4444-5555", place.PhoneNumber);

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("my-token", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains($"v2/actors/compass~crawler-google-places/run-sync-get-dataset-items", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchAsync_FallsBackToPhoneUnformatted_WhenPhoneMissing()
    {
        const string responseBody = """
        [
          { "title": "Taller Sur", "placeId": "p1", "phoneUnformatted": "+541122223333" }
        ]
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria(["taller"], ["Moreno"], 50));

        Assert.Single(results);
        Assert.Equal("+541122223333", results[0].PhoneNumber);
    }

    [Fact]
    public async Task SearchAsync_ItemWithoutTitle_IsSkipped()
    {
        const string responseBody = """[ { "placeId": "p1", "phone": "123456" } ]""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria(["taller"], ["Moreno"], 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MultipleLocalitiesAndKeywords_BuildsCrossProductSearchStrings()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        await client.SearchAsync(new ApifySearchCriteria(["taller", "gomería"], ["Moreno", "Merlo"], 50));

        var body = JsonDocument.Parse(handler.LastRequestBody!);
        var searchStrings = body.RootElement.GetProperty("searchStringsArray").EnumerateArray().Select(e => e.GetString()).ToList();

        Assert.Equal(4, searchStrings.Count);
        Assert.Contains("taller en Moreno, Argentina", searchStrings);
        Assert.Contains("taller en Merlo, Argentina", searchStrings);
        Assert.Contains("gomería en Moreno, Argentina", searchStrings);
        Assert.Contains("gomería en Merlo, Argentina", searchStrings);
    }

    [Fact]
    public async Task SearchAsync_NoLocalitiesOrKeywords_ReturnsEmptyWithoutCallingApify()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "[]");
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria([], ["Moreno"], 50));

        Assert.Empty(results);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SearchAsync_ApiError_ReturnsEmptyList()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Forbidden, """{"error":"denied"}""");
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria(["taller"], ["Moreno"], 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RunObjectInsteadOfDatasetArray_ReturnsEmptyWithoutThrowing()
    {
        // Si el run sync de Apify no termina dentro de los 300s duros del endpoint, la respuesta
        // es el objeto del run (status Running) en vez de un array de items del dataset.
        const string runObjectResponse = """{ "data": { "id": "run1", "status": "RUNNING" } }""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, runObjectResponse);
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria(["taller"], ["Moreno"], 50));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_DuplicatePlaceIds_AreDeduplicated()
    {
        const string responseBody = """
        [
          { "title": "Taller Sur", "placeId": "p1", "phone": "123456" },
          { "title": "Taller Sur", "placeId": "p1", "phone": "123456" }
        ]
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        var results = await client.SearchAsync(new ApifySearchCriteria(["taller"], ["Moreno"], 50));

        Assert.Single(results);
    }
}
