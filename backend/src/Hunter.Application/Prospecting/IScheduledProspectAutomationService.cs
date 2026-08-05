using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IScheduledProspectAutomationService
{
    Task<Result<ScheduledProspectAutomationDto>> CreateAsync(ScheduleProspectAutomationRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<ScheduledProspectAutomationDto>> ListAsync(CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default);

    // Ejecuta una automatización vencida: buscar en OSM, importar todo lo válido sin revisión,
    // sumarlo a la campaña y arrancar/continuar el envío. La llama el background service, nunca
    // un endpoint HTTP directo (por eso no está en el controller).
    Task RunAsync(int id, CancellationToken ct = default);
}
