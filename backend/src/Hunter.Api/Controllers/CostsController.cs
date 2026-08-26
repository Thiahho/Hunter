using Hunter.Application.Finance;
using Hunter.Application.Finance.Contracts;
using Hunter.Domain.Identity;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

// Costos de campaña (márgenes, gasto en Meta, etc.): información comercial sensible, reservada a
// roles de gestión (auditoria.md, hallazgo Medio "Sales/Costs sin restricción de rol").
[ApiController]
[Authorize(Roles = $"{RoleNames.Owner},{RoleNames.Admin},{RoleNames.Manager}")]
[Route("api/v1/costs")]
public class CostsController(ICostService costService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCostRequest request, CancellationToken ct)
    {
        var result = await costService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<CostDto>.Fail(result.Error!));

        return Ok(ApiResponse<CostDto>.Ok(result.Value!));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] int? campaignId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await costService.SearchAsync(campaignId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<CostDto>>.Ok(result));
    }
}
