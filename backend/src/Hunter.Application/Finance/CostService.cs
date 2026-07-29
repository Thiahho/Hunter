using Hunter.Application.Common;
using Hunter.Application.Finance.Contracts;
using Hunter.Domain.Finance;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Finance;

public class CostService(IHunterDbContext db, ICurrentUserService currentUser) : ICostService
{
    public async Task<Result<CostDto>> CreateAsync(CreateCostRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            return Result<CostDto>.Failure("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(request.Provider))
            return Result<CostDto>.Failure("El proveedor es obligatorio.");

        var cost = new Cost
        {
            OrganizationId = currentUser.OrganizationId!.Value,
            CampaignId = request.CampaignId,
            Type = request.Type,
            Provider = request.Provider.Trim(),
            ReferenceId = request.ReferenceId,
            Amount = request.Amount,
            Currency = request.Currency,
            Date = request.Date ?? DateTimeOffset.UtcNow
        };

        db.Costs.Add(cost);
        await db.SaveChangesAsync(ct);

        return Result<CostDto>.Success(ToDto(cost));
    }

    public async Task<PagedResult<CostDto>> SearchAsync(int? campaignId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var costs = db.Costs.AsQueryable();
        if (campaignId is not null)
            costs = costs.Where(c => c.CampaignId == campaignId);

        var totalItems = await costs.CountAsync(ct);

        var items = await costs
            .OrderByDescending(c => c.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToDto(c))
            .ToListAsync(ct);

        return new PagedResult<CostDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }

    private static CostDto ToDto(Cost c) => new(c.Id, c.Type, c.Provider, c.ReferenceId, c.CampaignId, c.Amount, c.Currency, c.Date);
}
