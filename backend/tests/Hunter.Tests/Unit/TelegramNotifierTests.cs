using System.Net;
using System.Text;
using System.Text.Json;
using Hunter.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class TelegramNotifierTests
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

    private static (TelegramNotifier Notifier, FakeHttpMessageHandler Handler) CreateNotifier(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") };
        var options = Options.Create(new TelegramOptions { BotToken = "test-token" });
        return (new TelegramNotifier(httpClient, options, NullLogger<TelegramNotifier>.Instance), handler);
    }

    [Fact]
    public async Task SendAsync_Success_PostsToCorrectUrlWithChatIdAndText()
    {
        var (notifier, handler) = CreateNotifier(HttpStatusCode.OK, """{"ok":true,"result":{}}""");

        var result = await notifier.SendAsync("555111222", "🔥 NUEVO LEAD");

        Assert.True(result.Success);
        Assert.Null(result.Error);

        Assert.Equal("https://api.telegram.org/bottest-token/sendMessage", handler.LastRequest!.RequestUri!.ToString());

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("555111222", doc.RootElement.GetProperty("chat_id").GetString());
        Assert.Equal("🔥 NUEVO LEAD", doc.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task SendAsync_ApiError_ReturnsFailureWithDescription()
    {
        var (notifier, _) = CreateNotifier(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: chat not found"}""");

        var result = await notifier.SendAsync("bad-chat-id", "hola");

        Assert.False(result.Success);
        Assert.Equal("Bad Request: chat not found", result.Error);
    }

    [Fact]
    public async Task SendAsync_UnparsableErrorBody_ReturnsFailureWithHttpStatus()
    {
        var (notifier, _) = CreateNotifier(HttpStatusCode.InternalServerError, "not json");

        var result = await notifier.SendAsync("555111222", "hola");

        Assert.False(result.Success);
        Assert.Equal("HTTP 500", result.Error);
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("network unreachable");
    }

    [Fact]
    public async Task SendAsync_NetworkError_ReturnsFailureWithoutThrowing()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler()) { BaseAddress = new Uri("https://api.telegram.org/") };
        var notifier = new TelegramNotifier(httpClient, Options.Create(new TelegramOptions { BotToken = "test-token" }), NullLogger<TelegramNotifier>.Instance);

        var result = await notifier.SendAsync("555111222", "hola");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }
}
