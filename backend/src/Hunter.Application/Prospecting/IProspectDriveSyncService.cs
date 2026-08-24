using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IProspectDriveSyncService
{
    // La llama ProspectDriveSyncBackgroundService periódicamente, nunca un endpoint HTTP directo
    // (mismo criterio que IScheduledProspectAutomationService.RunAsync).
    Task<Result<ProspectDriveSyncResultDto>> SyncAsync(CancellationToken ct = default);

    // Solo lectura, para que el frontend muestre el link — null si todavía no sincronizó nunca.
    Task<ProspectDriveSyncResultDto?> GetStatusAsync(CancellationToken ct = default);
}
