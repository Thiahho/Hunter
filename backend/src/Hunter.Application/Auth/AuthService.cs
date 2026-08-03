using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Auth;

public class AuthService(IHunterDbContext db, IPasswordHasher passwordHasher, IJwtTokenService tokenService) : IAuthService
{
    public async Task<Result<AuthResult>> RegisterOrganizationAsync(RegisterOrganizationRequest request, CancellationToken ct = default)
    {
        var validationError = ValidateRegister(request);
        if (validationError is not null)
            return Result<AuthResult>.Failure(validationError);

        var organization = new Organization
        {
            Name = request.OrganizationName.Trim()
        };

        var user = new User
        {
            OrganizationId = organization.Id,
            Organization = organization,
            FirstName = request.OwnerFirstName.Trim(),
            LastName = request.OwnerLastName.Trim(),
            Email = request.OwnerEmail.Trim().ToLowerInvariant(),
            // Una organización de una sola persona tiene que recibir sus propios leads
            // desde el arranque, sin depender de que alguien configure áreas primero.
            Area = UserArea.Ventas
        };
        user.PasswordHash = passwordHasher.Hash(user, request.Password);

        var ownerRole = new UserRole { UserId = user.Id, User = user, RoleId = RoleIds.Owner };

        db.Organizations.Add(organization);
        db.Users.Add(user);
        db.UserRoles.Add(ownerRole);

        await db.SaveChangesAsync(ct);

        var result = await BuildAuthResultAsync(user, [RoleNames.Owner], ct);
        return Result<AuthResult>.Success(result);
    }

    public async Task<Result<AuthResult>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !user.IsActive || !passwordHasher.Verify(user, user.PasswordHash, request.Password))
            return Result<AuthResult>.Failure("Credenciales inválidas.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var result = await BuildAuthResultAsync(user, roles, ct);
        return Result<AuthResult>.Success(result);
    }

    public async Task<Result<AuthResult>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var token = await db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null || !token.IsActive)
            return Result<AuthResult>.Failure("Refresh token inválido o expirado.");

        token.RevokedAt = DateTimeOffset.UtcNow;

        var roles = token.User.UserRoles.Select(ur => ur.Role.Name).ToList();
        var result = await BuildAuthResultAsync(token.User, roles, ct);

        return Result<AuthResult>.Success(result);
    }

    public async Task<Result<bool>> LogoutAsync(int currentUserId, string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(refreshToken);

        var token = await db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == currentUserId, ct);

        if (token is null)
            return Result<bool>.Failure("Refresh token no encontrado.");

        token.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<CurrentUserDto>.Failure("Usuario no encontrado.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return Result<CurrentUserDto>.Success(ToDto(user, roles));
    }

    private async Task<AuthResult> BuildAuthResultAsync(User user, IReadOnlyCollection<string> roles, CancellationToken ct)
    {
        var accessToken = tokenService.CreateAccessToken(user, roles);
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt
        });

        await db.SaveChangesAsync(ct);

        return new AuthResult(accessToken.AccessToken, accessToken.ExpiresAt, refreshToken.RawToken, ToDto(user, roles));
    }

    private static CurrentUserDto ToDto(User user, IReadOnlyCollection<string> roles) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.OrganizationId, roles);

    private static string? ValidateRegister(RegisterOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return "El nombre de la organización es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.OwnerFirstName) || string.IsNullOrWhiteSpace(request.OwnerLastName))
            return "El nombre y apellido del propietario son obligatorios.";
        if (string.IsNullOrWhiteSpace(request.OwnerEmail) || !request.OwnerEmail.Contains('@'))
            return "El email no es válido.";
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        return null;
    }
}
