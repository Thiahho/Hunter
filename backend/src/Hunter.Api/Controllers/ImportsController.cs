using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/imports")]
public class ImportsController(IImportService importService) : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(ApiResponse<ImportPreviewDto>.Fail("El archivo está vacío."));

        await using var stream = file.OpenReadStream();
        var result = await importService.ImportCsvAsync(stream, file.FileName, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ImportPreviewDto>.Fail(result.Error!));

        return Ok(ApiResponse<ImportPreviewDto>.Ok(result.Value!));
    }

    [HttpPost("google-places")]
    public async Task<IActionResult> ImportFromGooglePlaces(GooglePlacesImportRequest request, CancellationToken ct)
    {
        var result = await importService.ImportFromGooglePlacesAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ImportPreviewDto>.Fail(result.Error!));

        return Ok(ApiResponse<ImportPreviewDto>.Ok(result.Value!));
    }

    [HttpGet("{id:int}/preview")]
    public async Task<IActionResult> Preview(int id, CancellationToken ct)
    {
        var result = await importService.GetPreviewAsync(id, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<ImportPreviewDto>.Fail(result.Error!));

        return Ok(ApiResponse<ImportPreviewDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id, CancellationToken ct)
    {
        var result = await importService.ConfirmAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ImportConfirmResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<ImportConfirmResultDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await importService.CancelAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
