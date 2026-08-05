using Hunter.Application.Auth;
using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hunter.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterOrganizationRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterOrganizationAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResult>.Fail(result.Error!));

        return Ok(ApiResponse<AuthResult>.Ok(result.Value!));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResult>.Fail(result.Error!));

        return Ok(ApiResponse<AuthResult>.Ok(result.Value!));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request, ct);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResult>.Fail(result.Error!));

        return Ok(ApiResponse<AuthResult>.Ok(result.Value!));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.LogoutAsync(currentUserService.UserId!.Value, request.RefreshToken, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await authService.GetCurrentUserAsync(currentUserService.UserId!.Value, ct);
        if (!result.Succeeded)
            return NotFound(ApiResponse<CurrentUserDto>.Fail(result.Error!));

        return Ok(ApiResponse<CurrentUserDto>.Ok(result.Value!));
    }

    [HttpPatch("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe(UpdateOwnProfileRequest request, CancellationToken ct)
    {
        var result = await authService.UpdateOwnProfileAsync(currentUserService.UserId!.Value, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<CurrentUserDto>.Fail(result.Error!));

        return Ok(ApiResponse<CurrentUserDto>.Ok(result.Value!));
    }

    [HttpPost("me/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await authService.ChangePasswordAsync(currentUserService.UserId!.Value, request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<bool>.Fail(result.Error!));

        return Ok(ApiResponse<bool>.Ok(true));
    }

    [HttpPost("me/telegram-link")]
    [Authorize]
    public async Task<IActionResult> GenerateTelegramLink(CancellationToken ct)
    {
        var result = await authService.GenerateTelegramLinkAsync(currentUserService.UserId!.Value, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<TelegramLinkDto>.Fail(result.Error!));

        return Ok(ApiResponse<TelegramLinkDto>.Ok(result.Value!));
    }
}
