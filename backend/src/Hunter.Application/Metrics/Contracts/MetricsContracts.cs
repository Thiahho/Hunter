using Hunter.Domain.Campaigning;
using Hunter.Domain.Finance;

namespace Hunter.Application.Metrics.Contracts;

public record DashboardDto(
    int ProspectsFound,
    int ProspectsValid,
    int ProspectsContacted,
    int MessagesSent,
    int Responses,
    int Interested,
    int Leads,
    int SalesWon,
    int SalesLost,
    decimal Revenue,
    decimal CostTotal,
    decimal? ResponseRatePct,
    decimal? InterestRatePct,
    decimal? LeadConversionRatePct,
    decimal? SalesConversionRatePct,
    decimal? CostPerLead,
    decimal? CostPerSale,
    decimal? AverageTicket);

public record CampaignMetricsDto(
    int CampaignId,
    string Name,
    CampaignStatus Status,
    int Recipients,
    int Sent,
    int Responded,
    int Interested,
    int Leads,
    int SalesWon,
    decimal Revenue,
    decimal Cost,
    decimal? ResponseRatePct,
    decimal? InterestRatePct,
    decimal? LeadConversionRatePct,
    decimal? SalesConversionRatePct);

public record LeadsMetricsDto(int New, int InProgress, int Won, int Lost, int Unattended, double? AverageFirstResponseMinutes);

public record CostByTypeDto(CostType Type, decimal Amount);

public record CostsMetricsDto(
    decimal Total,
    IReadOnlyCollection<CostByTypeDto> ByType,
    decimal? CostPerProspect,
    decimal? CostPerMessage,
    decimal? CostPerLead,
    decimal? CostPerSale);
