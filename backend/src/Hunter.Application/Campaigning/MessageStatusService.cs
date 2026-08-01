using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

// Aplica las actualizaciones de estado que manda el webhook de estado de WhatsApp
// (sent/delivered/read/failed). Los eventos de Meta pueden llegar duplicados o fuera de
// orden (reintentos de webhook), así que el avance de estado es monótono: un "sent" tardío
// nunca pisa un "read" ya registrado.
public class MessageStatusService(IHunterDbContext db) : IMessageStatusService
{
    public async Task UpdateDeliveryStatusAsync(
        string externalMessageId,
        MessageStatus newStatus,
        DateTimeOffset timestamp,
        string? failureReason = null,
        CancellationToken ct = default)
    {
        var message = await db.Messages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.ExternalMessageId == externalMessageId, ct);

        // Puede llegar un status para un mensaje que no es nuestro (otro sistema usando el
        // mismo número) o para uno que todavía no terminamos de guardar; no es un error.
        if (message is null)
            return;

        if (!CanTransition(message.Status, newStatus))
            return;

        message.Status = newStatus;

        switch (newStatus)
        {
            case MessageStatus.Delivered:
                message.DeliveredAt = timestamp;
                break;
            case MessageStatus.Read:
                message.ReadAt = timestamp;
                break;
            case MessageStatus.Failed:
                message.FailedAt = timestamp;
                message.FailureReason = failureReason;
                break;
        }

        await db.SaveChangesAsync(ct);
    }

    // Read/Failed/Cancelled son terminales: una vez ahí, ningún evento tardío de Meta
    // (reintento de webhook, orden de entrega distinto) los pisa.
    private static bool CanTransition(MessageStatus from, MessageStatus to) => to switch
    {
        MessageStatus.Sent => from is MessageStatus.Pending,
        MessageStatus.Delivered => from is MessageStatus.Pending or MessageStatus.Sent,
        MessageStatus.Read => from is MessageStatus.Pending or MessageStatus.Sent or MessageStatus.Delivered,
        MessageStatus.Failed => from is MessageStatus.Pending or MessageStatus.Sent,
        _ => false
    };
}
