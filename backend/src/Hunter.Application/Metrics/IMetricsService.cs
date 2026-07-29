using Hunter.Application.Metrics.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Metrics;

public interface IMetricsService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
    Task<Result<CampaignMetricsDto>> GetCampaignMetricsAsync(int campaignId, CancellationToken ct = default);
    Task<LeadsMetricsDto> GetLeadsMetricsAsync(CancellationToken ct = default);
    Task<CostsMetricsDto> GetCostsMetricsAsync(CancellationToken ct = default);
}
