using Hunter.Domain.Finance;

namespace Hunter.Application.Finance.Contracts;

public record CostDto(int Id, CostType Type, string Provider, string? ReferenceId, int? CampaignId, decimal Amount, string Currency, DateTimeOffset Date);

public record CreateCostRequest(CostType Type, string Provider, decimal Amount, string Currency = "ARS", int? CampaignId = null, string? ReferenceId = null, DateTimeOffset? Date = null);
