using Hunter.Application.Auth;
using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Identity;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Owner)]
[Route("api/v1/users")]
public class UsersController(IUserService userService, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await userService.ListAsync(currentUserService.OrganizationId!.Value, ct);
        return Ok(ApiResponse<IReadOnlyCollection<UserDto>>.Ok(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken ct)
    {
        var result = await userService.CreateAsync(currentUserService.OrganizationId!.Value, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<UserDto>.Fail(result.Error!));

        return Ok(ApiResponse<UserDto>.Ok(result.Value!));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request, CancellationToken ct)
    {
        var result = await userService.UpdateAsync(currentUserService.OrganizationId!.Value, id, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<UserDto>.Fail(result.Error!));

        return Ok(ApiResponse<UserDto>.Ok(result.Value!));
    }
}
