using Hunter.Application.Sales;
using Hunter.Application.Sales.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
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
