namespace Hunter.Infrastructure.Messaging;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    // Token del bot dado por @BotFather. Sin esto, DependencyInjection registra
    // StubTelegramNotifier en su lugar (ver AddInfrastructure), igual que WhatsApp sin configurar.
    public string? BotToken { get; set; }

    // @usuario del bot (sin el @), necesario para armar el deep link t.me/<BotUsername>?start=<code>
    // del flujo de vinculación self-service. Se obtiene de @BotFather al crear el bot.
    public string? BotUsername { get; set; }

    // Valor arbitrario que también se configura en Telegram al registrar el webhook
    // (secret_token de setWebhook); Telegram lo devuelve en el header
    // X-Telegram-Bot-Api-Secret-Token de cada request, así se valida que el POST es de Telegram.
    public string? WebhookSecret { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken);
}
