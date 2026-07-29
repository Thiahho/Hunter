using System.Security.Cryptography;
using System.Text;

namespace Hunter.Infrastructure.Messaging;

// Valida la firma HMAC-SHA256 que Meta envía en X-Hub-Signature-256 para probar que el
// webhook realmente viene de Meta y no de un tercero (endpoint es AllowAnonymous).
public static class WhatsAppWebhookSignatureValidator
{
    public static bool IsValid(string rawBody, string? signatureHeader, string? appSecret)
    {
        if (string.IsNullOrEmpty(appSecret))
            return false;

        if (string.IsNullOrEmpty(signatureHeader) || !signatureHeader.StartsWith("sha256=", StringComparison.Ordinal))
            return false;

        var providedHash = signatureHeader["sha256=".Length..];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash),
            Encoding.UTF8.GetBytes(computedHash));
    }
}
