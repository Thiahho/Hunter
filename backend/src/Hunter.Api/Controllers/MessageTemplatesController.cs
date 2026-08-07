using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/templates")]
public class MessageTemplatesController(IMessageTemplateService templateService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var templates = await templateService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyCollection<MessageTemplateDto>>.Ok(templates));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await templateService.GetByIdAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<MessageTemplateDto>.Fail(result.Error!));

        return Ok(ApiResponse<MessageTemplateDto>.Ok(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMessageTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<MessageTemplateDto>.Fail(result.Error!));

        return Ok(ApiResponse<MessageTemplateDto>.Ok(result.Value!));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateMessageTemplateRequest request, CancellationToken ct)
    {
        var result = await templateService.UpdateAsync(id, request, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<MessageTemplateDto>.Fail(result.Error!));

        return Ok(ApiResponse<MessageTemplateDto>.Ok(result.Value!));
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] bool isActive, CancellationToken ct)
    {
        var result = await templateService.SetActiveAsync(id, isActive, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPatch("{id:int}/catalog")]
    public async Task<IActionResult> SetCatalog(int id, CancellationToken ct)
    {
        var result = await templateService.SetCatalogAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpGet("meta")]
    public async Task<IActionResult> ListMetaTemplates(CancellationToken ct)
    {
        var result = await templateService.ListMetaTemplatesAsync(ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<IReadOnlyList<MetaWhatsAppTemplateDto>>.Fail(result.Error!));

        return Ok(ApiResponse<IReadOnlyList<MetaWhatsAppTemplateDto>>.Ok(result.Value!));
    }

    [HttpPost("meta/sync")]
    public async Task<IActionResult> SyncFromMeta(SyncMessageTemplateFromMetaRequest request, CancellationToken ct)
    {
        var result = await templateService.SyncFromMetaAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<MessageTemplateDto>.Fail(result.Error!));

        return Ok(ApiResponse<MessageTemplateDto>.Ok(result.Value!));
    }
}
