using Hunter.Application.Auth;
using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Identity;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hunter.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IAuthService authService, ICurrentUserService currentUserService) : ControllerBase
{
    // Antes era [AllowAnonymous]: cualquiera en internet podía crear una organización nueva
    // (auditoria.md, hallazgo Bajo "registro sin fricción"). Crea una organización + usuario
    // Owner independiente de la del que llama, así que no importa a cuál organización
    // pertenezca quien la crea — solo que sea Owner o Admin de alguna.
    [HttpPost("register")]
    [Authorize(Roles = $"{RoleNames.Owner},{RoleNames.Admin}")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterOrganizationRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterOrganizationAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResult>.Fail(result.Error!));

        return Ok(ApiResponse<AuthResult>.Ok(result.Value!));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResult>.Fail(result.Error!));

        return Ok(ApiResponse<AuthResult>.Ok(result.Value!));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
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
