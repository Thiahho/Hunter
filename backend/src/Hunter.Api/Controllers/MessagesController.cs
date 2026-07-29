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
public class MessagesController(IMessageQueryService messageQueryService) : ControllerBase
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
}
