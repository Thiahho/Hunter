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

    [Theory]
    [InlineData("5491122602000", "541122602000")] // móvil argentino: se quita el "9"
    [InlineData("5491112345678", "541112345678")]
    [InlineData("5511987654321", "5511987654321")] // no argentino: queda igual
    [InlineData("541122602000", "541122602000")] // ya sin el "9": queda igual (idempotente)
    public void ToMetaWhatsAppFormat_StripsArgentineMobileNine(string stored, string expected)
    {
        Assert.Equal(expected, WhatsAppCloudApiMessageProvider.ToMetaWhatsAppFormat(stored));
    }

    [Fact]
    public async Task SendAsync_ArgentineMobileNumber_SendsWithoutTheNineToMeta()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.ABC"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions { PhoneNumberId = "123", AccessToken = "token" });

        await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "5491122602000", "hola"));

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("541122602000", doc.RootElement.GetProperty("to").GetString());
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
        Assert.Equal("541112345678", doc.RootElement.GetProperty("to").GetString()); // sin el "9" (quirk de Meta para Argentina)
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
    public async Task SendAsync_TemplateWithoutBodyParameter_OmitsComponents()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.NOPARAM"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general",
            TemplateLanguage = "es_AR",
            TemplateBodyParameterCount = 0
        });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "5491112345678", "contenido"));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("template", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("bienvenida_general", doc.RootElement.GetProperty("template").GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("template").TryGetProperty("components", out _));
    }

    [Fact]
    public async Task SendAsync_TemplateWithTwoBodyParameters_SendsNameAndStaticSecondParameter()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.TWOPARAMS"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general",
            TemplateLanguage = "es_AR",
            TemplateBodyParameterCount = 2,
            TemplateSecondParameter = "https://www.tauroparts.shop/catalog"
        });

        var result = await provider.SendAsync(new SendMessageRequest(
            MessagingChannel.Whatsapp, "5491112345678", "contenido libre", "Ferretería Sur"));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var parameters = doc.RootElement.GetProperty("template").GetProperty("components")[0].GetProperty("parameters");
        Assert.Equal(2, parameters.GetArrayLength());
        Assert.Equal("Ferretería Sur", parameters[0].GetProperty("text").GetString());
        Assert.Equal("https://www.tauroparts.shop/catalog", parameters[1].GetProperty("text").GetString());
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

    [Fact]
    public async Task SendAsync_TemplateWithQuickReplyPayload_SendsButtonComponentAfterBody()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.BUTTONS"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general",
            TemplateLanguage = "es_AR",
            TemplateBodyParameterCount = 0,
            TemplateQuickReplyPayloads = ["ESTOY_INTERESADO"]
        });

        var result = await provider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, "5491112345678", "contenido"));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var components = doc.RootElement.GetProperty("template").GetProperty("components");
        Assert.Equal(1, components.GetArrayLength()); // sin body (TemplateBodyParameterCount = 0), un solo botón

        var button0 = components[0];
        Assert.Equal("button", button0.GetProperty("type").GetString());
        Assert.Equal("quick_reply", button0.GetProperty("sub_type").GetString());
        Assert.Equal("0", button0.GetProperty("index").GetString());
        Assert.Equal("ESTOY_INTERESADO", button0.GetProperty("parameters")[0].GetProperty("payload").GetString());
    }

    [Fact]
    public async Task SendAsync_TemplateWithBodyParameterAndQuickReplyPayload_SendsBodyThenButtonComponent()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.BUTTONS2"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general",
            TemplateLanguage = "es_AR",
            TemplateBodyParameterCount = 1,
            TemplateQuickReplyPayloads = ["ESTOY_INTERESADO"]
        });

        var result = await provider.SendAsync(new SendMessageRequest(
            MessagingChannel.Whatsapp, "5491112345678", "contenido", "Ferretería Sur"));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var components = doc.RootElement.GetProperty("template").GetProperty("components");
        Assert.Equal(2, components.GetArrayLength()); // body + un botón

        Assert.Equal("body", components[0].GetProperty("type").GetString());

        var button0 = components[1];
        Assert.Equal("quick_reply", button0.GetProperty("sub_type").GetString());
        Assert.Equal("0", button0.GetProperty("index").GetString());
        Assert.Equal("ESTOY_INTERESADO", button0.GetProperty("parameters")[0].GetProperty("payload").GetString());
    }

    [Fact]
    public async Task SendAsync_PreferFreeText_IgnoresConfiguredTemplate()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.FREETEXT"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general"
        });

        var result = await provider.SendAsync(new SendMessageRequest(
            MessagingChannel.Whatsapp, "5491112345678", "el catalogo real", PreferFreeText: true));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("text", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("el catalogo real", doc.RootElement.GetProperty("text").GetProperty("body").GetString());
    }

    [Fact]
    public async Task SendAsync_TemplateNameOverride_SendsHandoffTemplateWithExplicitParameters()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK,
            """{"messages":[{"id":"wamid.HANDOFF"}]}""");
        var provider = CreateProvider(handler, new WhatsAppCloudApiOptions
        {
            PhoneNumberId = "123",
            AccessToken = "token",
            TemplateName = "bienvenida_general", // no debe usarse: el override manda
            HandoffTemplateLanguage = "es"
        });

        var result = await provider.SendAsync(new SendMessageRequest(
            MessagingChannel.Whatsapp, "5491112345678", "texto libre (ignorado)",
            TemplateNameOverride: "nuevo_lead",
            TemplateParameters: ["Repuestos Oeste", "Moreno", "Casa de repuestos", "92", "Me pasas info?"]));

        Assert.True(result.Success);

        using var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("template", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("nuevo_lead", doc.RootElement.GetProperty("template").GetProperty("name").GetString());
        Assert.Equal("es", doc.RootElement.GetProperty("template").GetProperty("language").GetProperty("code").GetString());

        var parameters = doc.RootElement.GetProperty("template").GetProperty("components")[0].GetProperty("parameters");
        Assert.Equal(5, parameters.GetArrayLength());
        Assert.Equal("Repuestos Oeste", parameters[0].GetProperty("text").GetString());

        // No debe llevar botones quick-reply: es una plantilla de notificación interna, no la de campaña.
        Assert.Equal(1, doc.RootElement.GetProperty("template").GetProperty("components").GetArrayLength());
    }
}
