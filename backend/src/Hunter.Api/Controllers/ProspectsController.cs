using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Crm;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/prospects")]
public class ProspectsController(
    IProspectService prospectService,
    ITestMessageService testMessageService,
    IScheduledMessageService scheduledMessageService,
    IManualTelegramAlertService manualTelegramAlertService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] ProspectCategory? category,
        [FromQuery] string? city,
        [FromQuery] string? province,
        [FromQuery] ProspectStatus? status,
        [FromQuery] string? tag,
        [FromQuery] ProspectSourceType? source,
        [FromQuery] BusinessSize? businessSize,
        [FromQuery] int? createdWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new ProspectQuery(search, category, city, province, status, tag, source, businessSize, createdWithinDays, page, pageSize);
        var result = await prospectService.SearchAsync(query, ct);
        return Ok(ApiResponse<PagedResult<ProspectListItemDto>>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await prospectService.GetByIdAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<ProspectDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProspectDto>.Ok(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProspectRequest request, CancellationToken ct)
    {
        var result = await prospectService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return Conflict(ApiResponse<ProspectDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProspectDto>.Ok(result.Value!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProspectRequest request, CancellationToken ct)
    {
        var result = await prospectService.UpdateAsync(id, request, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<ProspectDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProspectDto>.Ok(result.Value!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await prospectService.DeleteAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/test-message")]
    public async Task<IActionResult> SendTestMessage(int id, SendTestMessageRequest request, CancellationToken ct)
    {
        var result = await testMessageService.SendAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<TestMessageResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<TestMessageResultDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/telegram-alert")]
    public async Task<IActionResult> SendTelegramAlert(int id, CancellationToken ct)
    {
        var result = await manualTelegramAlertService.SendAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpGet("{id:int}/scheduled-messages")]
    public async Task<IActionResult> ListScheduledMessages(int id, CancellationToken ct)
    {
        var result = await scheduledMessageService.ListByProspectAsync(id, ct);
        return Ok(ApiResponse<IReadOnlyCollection<ScheduledMessageDto>>.Ok(result));
    }

    [HttpPost("{id:int}/scheduled-messages")]
    public async Task<IActionResult> ScheduleMessage(int id, ScheduleMessageRequest request, CancellationToken ct)
    {
        var result = await scheduledMessageService.CreateAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ScheduledMessageDto>.Fail(result.Error!));

        return Ok(ApiResponse<ScheduledMessageDto>.Ok(result.Value!));
    }

    [HttpPost("scheduled-messages/{scheduledMessageId:int}/cancel")]
    public async Task<IActionResult> CancelScheduledMessage(int scheduledMessageId, CancellationToken ct)
    {
        var result = await scheduledMessageService.CancelAsync(scheduledMessageId, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/tags")]
    public async Task<IActionResult> AddTag(int id, [FromBody] string tagName, CancellationToken ct)
    {
        var result = await prospectService.AddTagAsync(id, tagName, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpDelete("{id:int}/tags/{tagId:int}")]
    public async Task<IActionResult> RemoveTag(int id, int tagId, CancellationToken ct)
    {
        var result = await prospectService.RemoveTagAsync(id, tagId, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("{id:int}/contacts")]
    public async Task<IActionResult> AddContact(int id, AddProspectContactRequest request, CancellationToken ct)
    {
        var result = await prospectService.AddContactAsync(id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ProspectContactDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProspectContactDto>.Ok(result.Value!));
    }

    [HttpPut("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> UpdateContact(int id, int contactId, UpdateProspectContactRequest request, CancellationToken ct)
    {
        var result = await prospectService.UpdateContactAsync(id, contactId, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ProspectContactDto>.Fail(result.Error!));

        return Ok(ApiResponse<ProspectContactDto>.Ok(result.Value!));
    }

    [HttpDelete("{id:int}/contacts/{contactId:int}")]
    public async Task<IActionResult> RemoveContact(int id, int contactId, CancellationToken ct)
    {
        var result = await prospectService.RemoveContactAsync(id, contactId, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
