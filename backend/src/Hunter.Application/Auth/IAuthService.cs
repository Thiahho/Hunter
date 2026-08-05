using Hunter.Application.Auth.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResult>> RegisterOrganizationAsync(RegisterOrganizationRequest request, CancellationToken ct = default);
    Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResult>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result<bool>> LogoutAsync(int currentUserId, string refreshToken, CancellationToken ct = default);
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default);
    Task<Result<TelegramLinkDto>> GenerateTelegramLinkAsync(int userId, CancellationToken ct = default);
    Task<Result<bool>> CompleteTelegramLinkAsync(string code, string chatId, CancellationToken ct = default);
}
