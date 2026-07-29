using Hunter.Application.Finance.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Finance;

public interface ICostService
{
    Task<Result<CostDto>> CreateAsync(CreateCostRequest request, CancellationToken ct = default);
    Task<PagedResult<CostDto>> SearchAsync(int? campaignId, int page, int pageSize, CancellationToken ct = default);
}
