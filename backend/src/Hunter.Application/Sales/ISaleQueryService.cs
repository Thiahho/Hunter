using Hunter.Application.Sales.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Sales;

public interface ISaleQueryService
{
    Task<PagedResult<SaleDto>> SearchAsync(int? sellerId, int? campaignId, int page, int pageSize, CancellationToken ct = default);
}
