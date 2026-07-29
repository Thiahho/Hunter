using Hunter.Application.Compliance;
using Hunter.Application.Compliance.Contracts;
using Hunter.Domain.Compliance;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/suppressions")]
public class SuppressionsController(ISuppressionService suppressionService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await suppressionService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyCollection<SuppressionDto>>.Ok(items));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSuppressionRequest request, CancellationToken ct)
    {
        var result = await suppressionService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return Conflict(ApiResponse<SuppressionDto>.Fail(result.Error!));

        return Ok(ApiResponse<SuppressionDto>.Ok(result.Value!));
    }

    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] SuppressionContactType contactType, [FromQuery] string contact, CancellationToken ct)
    {
        var suppressed = await suppressionService.IsSuppressedAsync(contactType, contact, ct);
        return Ok(ApiResponse<bool>.Ok(suppressed));
    }
}
