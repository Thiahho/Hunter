using System.Net;
using System.Text;
using System.Text.Json;
using Hunter.Application.Campaigning;
using Hunter.Domain.Campaigning;
using Hunter.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Unit;

public class WhatsAppCloudApiMessageProviderTests
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

    private static WhatsAppCloudApiMessageProvider CreateProvider(
        FakeHttpMessageHandler handler, WhatsAppCloudApiOptions options)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://graph.facebook.com/") };
        return new WhatsAppCloudApiMessageProvider(
            httpClient, Options.Create(options), NullLogger<WhatsAppCloudApiMessageProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_NonWhatsappChannel_ReturnsFailureWithoutCallingApi()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions { PhoneNumberId = "123", AccessToken = "token" });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Telegram, "5491112345678", "hola"));

        Assert.False(result.Success);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task SendAsync_NoTemplateConfigured_SendsFreeTextPayload()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.ABC123"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions { PhoneNumberId = "123", AccessToken = "token" });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "5491112345678", "Hola, te contactamos de Repuestos Oeste"));

        Assert.True(result.Success);
        Assert.Equal("wamid.ABC123", result.ExternalMessageId);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("text", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("5491112345678", doc.RootElement.GetProperty("to").GetString());
        Assert.Equal("Hola, te contactamos de Repuestos Oeste", doc.RootElement.GetProperty("text").GetProperty("body").GetString());
    }

    [Fact]
    public async Task SendAsync_TemplateConfigured_SendsTemplatePayload()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.XYZ789"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "primer_contacto",
            TemplateLanguage = "es_AR"
        });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "5491112345678", "contenido"));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("template", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("primer_contacto", doc.RootElement.GetProperty("template").GetProperty("name").GetString());
        Assert.Equal("es_AR", doc.RootElement.GetProperty("template").GetProperty("language").GetProperty("code").GetString());
    }

    [Fact]
    public async Task SendAsync_ApiReturnsError_ReturnsFailureWithMessage()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.BadRequest,
            """{"error":{"message":"Invalid phone number"}}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions { PhoneNumberId = "123", AccessToken = "token" });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "no-es-un-telefono", "hola"));

        Assert.False(result.Success);
        Assert.Equal("Invalid phone number", result.Error);
    }
}
