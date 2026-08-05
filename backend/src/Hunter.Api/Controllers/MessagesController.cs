using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/messages")]
public class MessagesController(IMessageQueryService messageQueryService, IMessageResponseQueryService messageResponseQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? campaignId,
        [FromQuery] int? prospectId,
        [FromQuery] MessageStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await messageQueryService.SearchAsync(campaignId, prospectId, status, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<MessageDto>>.Ok(result));
    }

    // Respuestas entrantes (texto libre o tap de botón quick-reply) ya clasificadas por
    // IIntentClassifier — ver InboundMessageService.ProcessAsync. Endpoint separado de Search
    // porque Message (salida) y MessageResponse (entrada) son entidades distintas.
    [HttpGet("responses")]
    public async Task<IActionResult> SearchResponses(
        [FromQuery] int? campaignId,
        [FromQuery] int? prospectId,
        [FromQuery] IntentClassification? classification,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await messageResponseQueryService.SearchAsync(campaignId, prospectId, classification, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<MessageResponseDto>>.Ok(result));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await messageQueryService.DeleteAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete(BulkDeleteMessagesRequest request, CancellationToken ct)
    {
        var result = await messageQueryService.DeleteManyAsync(request.Ids, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<int>.Fail(result.Error!));

        return Ok(ApiResponse<int>.Ok(result.Value));
    }
}
