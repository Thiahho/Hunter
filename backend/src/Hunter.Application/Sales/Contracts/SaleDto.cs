using Hunter.Domain.Sales;

namespace Hunter.Application.Sales.Contracts;

public record SaleDto(
    int Id,
    int LeadId,
    int ProspectId,
    string ProspectBusinessName,
    int? CampaignId,
    int SellerId,
    decimal Amount,
    string Currency,
    decimal? Margin,
    string? ProductCategory,
    SaleStatus Status,
    DateTimeOffset Date);
