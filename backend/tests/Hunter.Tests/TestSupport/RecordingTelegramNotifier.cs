using Hunter.Application.Crm;

namespace Hunter.Tests.TestSupport;

// Mismo rol que RecordingMessageProvider pero para el canal de Telegram: graba cada envío para
// poder asertar chat_id/contenido, y permite simular fallos sin romper el flujo que los dispara.
public class RecordingTelegramNotifier : ITelegramNotifier
{
    public List<(string ChatId, string Message, TelegramButton? Button)> SentMessages { get; } = [];

    // null por defecto = Telegram no configurado (mismo comportamiento que StubTelegramNotifier);
    // los tests que necesitan simular un bot configurado lo setean explícitamente.
    public string? BotUsername { get; set; }

    public bool NextSendSucceeds { get; set; } = true;
    public string? NextSendError { get; set; }
    public bool ThrowOnNextSend { get; set; }

    public Task<TelegramSendResult> SendAsync(string chatId, string message, TelegramButton? button = null, CancellationToken ct = default)
    {
        SentMessages.Add((chatId, message, button));

        if (ThrowOnNextSend)
            throw new InvalidOperationException("Fallo simulado de Telegram.");

        return Task.FromResult(NextSendSucceeds
            ? new TelegramSendResult(true, null)
            : new TelegramSendResult(false, NextSendError ?? "Fallo simulado."));
    }
}
