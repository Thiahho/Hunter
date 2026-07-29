using System.Security.Cryptography;
using System.Text;
using Hunter.Infrastructure.Messaging;

namespace Hunter.Tests.Unit;

public class WhatsAppWebhookSignatureValidatorTests
{
    private const string AppSecret = "test-app-secret";

    [Fact]
    public void IsValid_CorrectSignature_ReturnsTrue()
    {
        var body = "{\"object\":\"whatsapp_business_account\"}";
        var signature = ComputeSignatureHeader(body, AppSecret);

        var result = WhatsAppWebhookSignatureValidator.IsValid(body, signature, AppSecret);

        Assert.True(result);
    }

    [Fact]
    public void IsValid_TamperedBody_ReturnsFalse()
    {
        var originalBody = "{\"object\":\"whatsapp_business_account\"}";
        var signature = ComputeSignatureHeader(originalBody, AppSecret);

        var result = WhatsAppWebhookSignatureValidator.IsValid("{\"object\":\"tampered\"}", signature, AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_WrongAppSecret_ReturnsFalse()
    {
        var body = "{\"object\":\"whatsapp_business_account\"}";
        var signature = ComputeSignatureHeader(body, "otro-secreto");

        var result = WhatsAppWebhookSignatureValidator.IsValid(body, signature, AppSecret);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-header")]
    [InlineData("sha1=abcdef")]
    public void IsValid_MalformedOrMissingHeader_ReturnsFalse(string? header)
    {
        var result = WhatsAppWebhookSignatureValidator.IsValid("{}", header, AppSecret);

        Assert.False(result);
    }

    [Fact]
    public void IsValid_MissingAppSecret_ReturnsFalse()
    {
        var body = "{}";
        var signature = ComputeSignatureHeader(body, AppSecret);

        var result = WhatsAppWebhookSignatureValidator.IsValid(body, signature, appSecret: null);

        Assert.False(result);
    }

    private static string ComputeSignatureHeader(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return $"sha256={hash}";
    }
}
