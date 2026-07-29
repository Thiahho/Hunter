using Hunter.Application.Metrics;
using Hunter.Application.Metrics.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/metrics")]
public class MetricsController(IMetricsService metricsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var result = await metricsService.GetDashboardAsync(ct);
        return Ok(ApiResponse<DashboardDto>.Ok(result));
    }

    [HttpGet("campaigns/{id:int}")]
    public async Task<IActionResult> CampaignMetrics(int id, CancellationToken ct)
    {
        var result = await metricsService.GetCampaignMetricsAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<CampaignMetricsDto>.Fail(result.Error!));

        return Ok(ApiResponse<CampaignMetricsDto>.Ok(result.Value!));
    }

    [HttpGet("leads")]
    public async Task<IActionResult> LeadsMetrics(CancellationToken ct)
    {
        var result = await metricsService.GetLeadsMetricsAsync(ct);
        return Ok(ApiResponse<LeadsMetricsDto>.Ok(result));
    }

    [HttpGet("costs")]
    public async Task<IActionResult> CostsMetrics(CancellationToken ct)
    {
        var result = await metricsService.GetCostsMetricsAsync(ct);
        return Ok(ApiResponse<CostsMetricsDto>.Ok(result));
    }
}
