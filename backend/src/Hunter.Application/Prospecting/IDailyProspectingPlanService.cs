using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IDailyProspectingPlanService
{
    Task<Result<DailyProspectingPlanDto>> CreateAsync(CreateDailyProspectingPlanRequest request, CancellationToken ct = default);
}
