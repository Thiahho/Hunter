using System.Security.Cryptography;
using System.Text;

namespace Hunter.Infrastructure.Messaging;

// Valida el header X-Telegram-Bot-Api-Secret-Token contra el secret_token configurado al
// registrar el webhook (setWebhook) — así se prueba que el POST viene de Telegram y no de un
// tercero (el endpoint es AllowAnonymous). A diferencia de WhatsApp, Telegram no firma el body
// con HMAC: solo hace eco del secret_token tal cual, por eso acá alcanza con comparar en tiempo
// constante en vez de recalcular un hash.
public static class TelegramWebhookSecretValidator
{
    public static bool IsValid(string? providedSecret, string? expectedSecret)
    {
        if (string.IsNullOrEmpty(expectedSecret) || string.IsNullOrEmpty(providedSecret))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedSecret),
            Encoding.UTF8.GetBytes(expectedSecret));
    }
}
