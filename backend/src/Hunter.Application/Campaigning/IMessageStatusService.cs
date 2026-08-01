using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning;

public interface IMessageStatusService
{
    Task UpdateDeliveryStatusAsync(
        string externalMessageId,
        MessageStatus newStatus,
        DateTimeOffset timestamp,
        string? failureReason = null,
        CancellationToken ct = default);
}
