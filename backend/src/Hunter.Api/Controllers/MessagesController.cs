using System.Text;
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
public class MessagesController(
    IMessageQueryService messageQueryService,
    IMessageResponseQueryService messageResponseQueryService,
    ITestMessageService testMessageService) : ControllerBase
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

    [HttpGet("daily")]
    public async Task<IActionResult> SearchDaily(
        [FromQuery] DateOnly? date,
        [FromQuery] string? province,
        [FromQuery] string? city,
        [FromQuery] bool? sent,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await messageQueryService.SearchDailyAsync(date, province, city, sent, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<DailyProspectDto>>.Ok(result));
    }

    [HttpGet("export/failed")]
    public async Task<IActionResult> ExportFailed(
        [FromQuery] DateOnly? date,
        [FromQuery] string? province,
        [FromQuery] string? city,
        CancellationToken ct = default)
    {
        var rows = await messageQueryService.GetFailedContactsAsync(date, province, city, ct);
        var csv = BuildFailedContactsCsv(rows);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"no-contactados-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string BuildFailedContactsCsv(IReadOnlyCollection<FailedContactDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nombre,Provincia,Localidad,Telefono,Motivo de falla,Fecha de falla");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(r.BusinessName),
                CsvEscape(r.Province),
                CsvEscape(r.City),
                CsvEscape(r.Phone),
                CsvEscape(r.FailureReason),
                CsvEscape(r.FailedAt?.ToString("yyyy-MM-dd HH:mm"))));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    [HttpPost("{id:int}/retry")]
    public async Task<IActionResult> Retry(int id, CancellationToken ct)
    {
        var result = await testMessageService.RetryAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<TestMessageResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<TestMessageResultDto>.Ok(result.Value!));
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

    [HttpDelete("responses/{id:int}")]
    public async Task<IActionResult> DeleteResponse(int id, CancellationToken ct)
    {
        var result = await messageResponseQueryService.DeleteAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("responses/bulk-delete")]
    public async Task<IActionResult> BulkDeleteResponses(BulkDeleteMessagesRequest request, CancellationToken ct)
    {
        var result = await messageResponseQueryService.DeleteManyAsync(request.Ids, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<int>.Fail(result.Error!));

        return Ok(ApiResponse<int>.Ok(result.Value));
    }
}
