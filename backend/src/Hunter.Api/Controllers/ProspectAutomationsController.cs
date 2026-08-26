using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

// Automatizaciones de un solo disparo: "a esta hora, buscá (en OSM o Apify) con estos criterios,
// importá todo lo válido y sumalo/arrancá esta campaña" — ver
// ScheduledProspectAutomationService.RunAsync y ScheduledProspectAutomationBackgroundService
// (quien realmente las ejecuta).
[ApiController]
[Authorize]
[Route("api/v1/prospect-automations")]
public class ProspectAutomationsController(IScheduledProspectAutomationService automationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await automationService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyCollection<ScheduledProspectAutomationDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScheduleProspectAutomationRequest request, CancellationToken ct)
    {
        var result = await automationService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<ScheduledProspectAutomationDto>.Fail(result.Error!));

        return Ok(ApiResponse<ScheduledProspectAutomationDto>.Ok(result.Value!));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await automationService.CancelAsync(id, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }
}
