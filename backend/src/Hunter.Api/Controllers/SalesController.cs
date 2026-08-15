using Hunter.Application.Sales;
using Hunter.Application.Sales.Contracts;
using Hunter.Domain.Identity;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

// Montos de venta y comisiones de todo el equipo: información comercial sensible, reservada a
// roles de gestión (auditoria.md, hallazgo Medio "Sales/Costs sin restricción de rol").
[ApiController]
[Authorize(Roles = $"{RoleNames.Owner},{RoleNames.Admin},{RoleNames.Manager}")]
[Route("api/v1/sales")]
public class SalesController(ISaleQueryService saleQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int? sellerId,
        [FromQuery] int? campaignId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await saleQueryService.SearchAsync(sellerId, campaignId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<SaleDto>>.Ok(result));
    }
}
