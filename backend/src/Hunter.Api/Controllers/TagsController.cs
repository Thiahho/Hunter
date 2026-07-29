using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tags")]
public class TagsController(ITagService tagService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tags = await tagService.ListAsync(ct);
        return Ok(ApiResponse<IReadOnlyCollection<TagDto>>.Ok(tags));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken ct)
    {
        var result = await tagService.CreateAsync(request, ct);
        if (!result.Succeeded)
            return Conflict(ApiResponse<TagDto>.Fail(result.Error!));

        return Ok(ApiResponse<TagDto>.Ok(result.Value!));
    }
}
