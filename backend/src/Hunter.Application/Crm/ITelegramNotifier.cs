namespace Hunter.Application.Crm;

public record TelegramSendResult(bool Success, string? Error);

// Abstracción del canal de alerta interno por Telegram (doc 23, sección 37 ampliada): un DM
// privado al vendedor/administrativo asignado cuando se crea un lead nuevo, en paralelo al
// aviso por WhatsApp. Vive en Application (no en Infrastructure) por el mismo motivo que
// IMessageProvider: InboundMessageService no puede depender directamente del proveedor.
public interface ITelegramNotifier
{
    // Expone solo el @usuario del bot (no todo TelegramOptions): Application no puede depender
    // de Infrastructure, mismo motivo por el que IMessageProvider expone HandoffTemplateName en
    // vez de las opciones completas de WhatsApp. null = Telegram no está configurado.
    string? BotUsername => null;

    Task<TelegramSendResult> SendAsync(string chatId, string message, CancellationToken ct = default);
}
