using System.Text.Json;
using Hunter.Api.Contracts;

namespace Hunter.Tests.Unit;

public class WhatsAppInboundExtractorTests
{
    [Fact]
    public void TryExtract_TextMessage_ReturnsContentFromBody()
    {
        var message = new WhatsAppWebhookMessage
        {
            From = "5491112345678",
            Id = "wamid.TEXT1",
            Type = "text",
            Text = new WhatsAppWebhookText { Body = "me interesa" }
        };

        var result = WhatsAppInboundExtractor.TryExtract(message);

        Assert.NotNull(result);
        Assert.Equal("me interesa", result.Content);
        Assert.Null(result.ButtonPayload);
        Assert.Null(result.ContextMessageId);
    }

    // Payload real de Meta (recortado a lo relevante) para un tap de botón quick_reply de
    // plantilla, incluyendo el "context" que referencia el wamid del mensaje saliente original.
    [Fact]
    public void TryExtract_RealMetaButtonPayload_ExtractsPayloadAndContext()
    {
        const string json = """
        {
            "from": "5491112345678",
            "id": "wamid.HBgNNTQ5MTEzODQ2OTY1MRUCABEYEkVCQzMzRDlBODZDMzUxOEJDRgA=",
            "timestamp": "1700000000",
            "type": "button",
            "button": {
                "payload": "RUBRO_MAYORISTA",
                "text": "Soy Mayorista"
            },
            "context": {
                "from": "5491100000000",
                "id": "wamid.ORIGINAL123"
            }
        }
        """;

        var message = JsonSerializer.Deserialize<WhatsAppWebhookMessage>(json)!;
        var result = WhatsAppInboundExtractor.TryExtract(message);

        Assert.NotNull(result);
        Assert.Equal("Soy Mayorista", result.Content);
        Assert.Equal("RUBRO_MAYORISTA", result.ButtonPayload);
        Assert.Equal("wamid.ORIGINAL123", result.ContextMessageId);
        Assert.Equal("wamid.HBgNNTQ5MTEzODQ2OTY1MRUCABEYEkVCQzMzRDlBODZDMzUxOEJDRgA=", result.Id);
    }

    [Fact]
    public void TryExtract_ButtonWithoutPayload_FallsBackToText()
    {
        var message = new WhatsAppWebhookMessage
        {
            From = "5491112345678",
            Id = "wamid.NOPAYLOAD",
            Type = "button",
            Button = new WhatsAppWebhookButton { Text = "Tengo Casa de Repuestos" }
        };

        var result = WhatsAppInboundExtractor.TryExtract(message);

        Assert.NotNull(result);
        Assert.Equal("Tengo Casa de Repuestos", result.Content);
        Assert.Equal("Tengo Casa de Repuestos", result.ButtonPayload);
    }

    [Fact]
    public void TryExtract_InteractiveButtonReply_ExtractsIdAndTitle()
    {
        var message = new WhatsAppWebhookMessage
        {
            From = "5491112345678",
            Id = "wamid.INTERACTIVE1",
            Type = "interactive",
            Interactive = new WhatsAppWebhookInteractive
            {
                Type = "button_reply",
                ButtonReply = new WhatsAppWebhookButtonReply { Id = "RUBRO_MAYORISTA", Title = "Soy Mayorista" }
            }
        };

        var result = WhatsAppInboundExtractor.TryExtract(message);

        Assert.NotNull(result);
        Assert.Equal("Soy Mayorista", result.Content);
        Assert.Equal("RUBRO_MAYORISTA", result.ButtonPayload);
    }

    [Fact]
    public void TryExtract_UnsupportedType_ReturnsNull()
    {
        var message = new WhatsAppWebhookMessage { From = "5491112345678", Id = "wamid.IMG1", Type = "image" };

        Assert.Null(WhatsAppInboundExtractor.TryExtract(message));
    }

    [Fact]
    public void TryExtract_ButtonTypeWithNullButton_ReturnsNull()
    {
        var message = new WhatsAppWebhookMessage { From = "5491112345678", Id = "wamid.MALFORMED", Type = "button", Button = null };

        Assert.Null(WhatsAppInboundExtractor.TryExtract(message));
    }

    [Fact]
    public void TryExtract_TextTypeWithNullText_ReturnsNull()
    {
        var message = new WhatsAppWebhookMessage { From = "5491112345678", Id = "wamid.MALFORMED2", Type = "text", Text = null };

        Assert.Null(WhatsAppInboundExtractor.TryExtract(message));
    }
}
