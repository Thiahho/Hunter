using System.Net;
using System.Text;
using Hunter.Infrastructure.Prospecting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class NominatimClientTests
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

    private static (NominatimClient Client, FakeHttpMessageHandler Handler) CreateClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new NominatimOptions { UserAgent = "HunterCRM/Test" });
        return (new NominatimClient(httpClient, options, NullLogger<NominatimClient>.Instance), handler);
    }

    [Fact]
    public async Task GeocodeAsync_ValidResponse_ParsesLatLon()
    {
        const string responseBody = """[{ "lat": "-34.6547", "lon": "-58.7205" }]""";
        var (client, handler) = CreateClient(HttpStatusCode.OK, responseBody);

        var result = await client.GeocodeAsync("Moreno, Argentina");

        Assert.NotNull(result);
        Assert.Equal(-34.6547, result!.Value.Lat, precision: 4);
        Assert.Equal(-58.7205, result.Value.Lon, precision: 4);
        Assert.Equal("HunterCRM/Test", handler.LastRequest!.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task GeocodeAsync_EmptyArray_ReturnsNull()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK, "[]");

        var result = await client.GeocodeAsync("LocalidadInexistente, Argentina");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_HttpError_ReturnsNullWithoutThrowing()
    {
        var (client, _) = CreateClient(HttpStatusCode.TooManyRequests, "");

        var result = await client.GeocodeAsync("Moreno, Argentina");

        Assert.Null(result);
    }

    [Fact]
    public async Task GeocodeAsync_MalformedJson_ReturnsNullWithoutThrowing()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK, "not json");

        var result = await client.GeocodeAsync("Moreno, Argentina");

        Assert.Null(result);
    }
}
