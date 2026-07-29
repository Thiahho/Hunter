namespace Hunter.Application.Auth.Contracts;

public record AuthResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    CurrentUserDto User);
