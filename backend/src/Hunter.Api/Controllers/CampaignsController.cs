using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/campaigns")]
public class CampaignsController(ICampaignService campaignService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] CampaignStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await campaignService.SearchAsync(status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<CampaignListItemDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await campaignService.GetByIdAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<CampaignDto>.Fail(result.Error!));

        return Ok(ApiResponse<CampaignDto>.Ok(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCampaignRequest request, CancellationToken ct)
    {
        var result = await campaignService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<CampaignDto>.Fail(result.Error!));

        return Ok(ApiResponse<CampaignDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/recipients")]
    public async Task<IActionResult> AddRecipients(int id, AddRecipientsRequest request, CancellationToken ct)
    {
        var result = await campaignService.AddRecipientsAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AddRecipientsResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<AddRecipientsResultDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/recipients/from-segment")]
    public async Task<IActionResult> AddRecipientsFromSegment(int id, AddRecipientsFromSegmentRequest request, CancellationToken ct)
    {
        var result = await campaignService.AddRecipientsFromSegmentAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AddRecipientsResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<AddRecipientsResultDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id, CancellationToken ct)
    {
        var result = await campaignService.StartAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/pause")]
    public async Task<IActionResult> Pause(int id, CancellationToken ct)
    {
        var result = await campaignService.PauseAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await campaignService.CancelAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/process-queue")]
    public async Task<IActionResult> ProcessQueue(int id, [FromQuery] int batchSize = 50, CancellationToken ct = default)
    {
        var result = await campaignService.ProcessQueueAsync(id, batchSize, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ProcessQueueResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProcessQueueResultDto>.Ok(result.Value!));
    }

    [HttpGet("recipients")]
    public async Task<IActionResult> SearchRecipients(
        [FromQuery] int? campaignId, [FromQuery] CampaignRecipientStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await campaignService.SearchRecipientsAsync(campaignId, status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<CampaignRecipientDto>>.Ok(result));
    }

    [HttpPost("recipients/retry")]
    public async Task<IActionResult> RetryRecipients(RetryRecipientsRequest request, CancellationToken ct)
    {
        var result = await campaignService.RetryRecipientsAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<RetryRecipientsResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<RetryRecipientsResultDto>.Ok(result.Value!));
    }

    [HttpPost("kill-switch")]
    public async Task<IActionResult> SetKillSwitch(KillSwitchRequest request, CancellationToken ct)
    {
        await campaignService.SetKillSwitchAsync(request, ct);
        return Ok(ApiResponse<bool>.Ok(request.Enabled));
    }

    [HttpGet("kill-switch")]
    public async Task<IActionResult> GetKillSwitch(CancellationToken ct)
    {
        var enabled = await campaignService.IsKillSwitchEnabledAsync(ct);
        return Ok(ApiResponse<bool>.Ok(enabled));
    }
}
