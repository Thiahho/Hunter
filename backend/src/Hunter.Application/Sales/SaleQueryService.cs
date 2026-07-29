using Hunter.Application.Common;
using Hunter.Application.Sales.Contracts;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Sales;

public class SaleQueryService(IHunterDbContext db) : ISaleQueryService
{
    public async Task<PagedResult<SaleDto>> SearchAsync(int? sellerId, int? campaignId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var sales = db.Sales.AsQueryable();
        if (sellerId is not null)
            sales = sales.Where(s => s.SellerId == sellerId);
        if (campaignId is not null)
            sales = sales.Where(s => s.CampaignId == campaignId);

        var totalItems = await sales.CountAsync(ct);

        var items = await sales
            .OrderByDescending(s => s.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SaleDto(
                s.Id, s.LeadId, s.ProspectId, s.Prospect.BusinessName, s.CampaignId, s.SellerId,
                s.Amount, s.Currency, s.Margin, s.ProductCategory, s.Status, s.Date))
            .ToListAsync(ct);

        return new PagedResult<SaleDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }
}
