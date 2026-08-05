using Hunter.Application.Crm;
using Microsoft.Extensions.Logging;

namespace Hunter.Infrastructure.Messaging;

// Placeholder para cuando Telegram:BotToken no está configurado. No manda nada real: solo
// registra, igual que StubMessageProvider para WhatsApp sin configurar.
public class StubTelegramNotifier(ILogger<StubTelegramNotifier> logger) : ITelegramNotifier
{
    public Task<TelegramSendResult> SendAsync(string chatId, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[StubTelegramNotifier] chat_id {ChatId}: {Message}", chatId, message);
        return Task.FromResult(new TelegramSendResult(true, null));
    }
}
