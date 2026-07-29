using Hunter.Application.Crm;
using Hunter.Application.Crm.Contracts;
using Hunter.Domain.Crm;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public class LeadsController(ILeadService leadService) : ControllerBase
{
    [HttpGet("leads")]
    public async Task<IActionResult> Search(
        [FromQuery] LeadStatus? status,
        [FromQuery] LeadPriority? priority,
        [FromQuery] int? assignedUserId,
        [FromQuery] int? campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new LeadQuery(status, priority, assignedUserId, campaignId, page, pageSize);
        var result = await leadService.SearchAsync(query, ct);
        return Ok(ApiResponse<PagedResult<LeadListItemDto>>.Ok(result));
    }

    [HttpGet("leads/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await leadService.GetByIdAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<LeadDto>.Fail(result.Error!));

        return Ok(ApiResponse<LeadDto>.Ok(result.Value!));
    }

    [HttpPost("leads/{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignLeadRequest request, CancellationToken ct)
    {
        var result = await leadService.AssignAsync(id, request, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("leads/{id:int}/in-progress")]
    public async Task<IActionResult> SetInProgress(int id, CancellationToken ct)
    {
        var result = await leadService.SetInProgressAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("leads/{id:int}/activities")]
    public async Task<IActionResult> AddActivity(int id, CreateLeadActivityRequest request, CancellationToken ct)
    {
        var result = await leadService.AddActivityAsync(id, request, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<LeadActivityDto>.Fail(result.Error!));

        return Ok(ApiResponse<LeadActivityDto>.Ok(result.Value!));
    }

    [HttpPost("leads/{id:int}/followups")]
    public async Task<IActionResult> AddFollowUp(int id, CreateFollowUpRequest request, CancellationToken ct)
    {
        var result = await leadService.AddFollowUpAsync(id, request, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<FollowUpDto>.Fail(result.Error!));

        return Ok(ApiResponse<FollowUpDto>.Ok(result.Value!));
    }

    [HttpPost("followups/{id:int}/complete")]
    public async Task<IActionResult> CompleteFollowUp(int id, CancellationToken ct)
    {
        var result = await leadService.CompleteFollowUpAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("leads/{id:int}/won")]
    public async Task<IActionResult> MarkWon(int id, MarkWonRequest request, CancellationToken ct)
    {
        var result = await leadService.MarkWonAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("leads/{id:int}/lost")]
    public async Task<IActionResult> MarkLost(int id, MarkLostRequest request, CancellationToken ct)
    {
        var result = await leadService.MarkLostAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
