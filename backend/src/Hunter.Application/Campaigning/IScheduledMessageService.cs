using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IScheduledMessageService
{
    Task<Result<ScheduledMessageDto>> CreateAsync(int prospectId, ScheduleMessageRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<ScheduledMessageDto>> ListByProspectAsync(int prospectId, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default);

    // Llamado por ScheduledMessageBackgroundService cuando ScheduledAt ya venció. Nunca lanza:
    // cualquier fallo queda en FailureReason con Status=Failed (mismo criterio que
    // ScheduledProspectAutomationService.RunAsync).
    Task RunAsync(int id, CancellationToken ct = default);
}
