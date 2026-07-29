using Hunter.Domain.Identity;

namespace Hunter.Application.Auth;

public record AccessTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public record RefreshTokenResult(string RawToken, string TokenHash, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles);
    RefreshTokenResult CreateRefreshToken();
    string HashRefreshToken(string rawToken);
}
