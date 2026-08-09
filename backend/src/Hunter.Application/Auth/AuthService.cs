using System.Security.Cryptography;
using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Crm;
using Hunter.Application.Prospecting;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Auth;

public class AuthService(
    IHunterDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService,
    ITelegramNotifier telegramNotifier) : IAuthService
{
    private static readonly TimeSpan TelegramLinkCodeLifetime = TimeSpan.FromMinutes(15);

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
            LastName = request.OwnerLastName?.Trim() ?? string.Empty,
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

    // Autoservicio, sin Role/Area/IsActive (eso lo administra OWNER/ADMIN vía UserService): el
    // propio usuario logueado edita sus datos personales y, si no quiere pasar por el flujo
    // /start del bot, puede pegar su chat_id de Telegram a mano acá también.
    public async Task<Result<CurrentUserDto>> UpdateOwnProfileAsync(int userId, UpdateOwnProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Result<CurrentUserDto>.Failure("Usuario no encontrado.");

        if (request.FirstName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName))
                return Result<CurrentUserDto>.Failure("El nombre no puede estar vacío.");
            user.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.LastName))
                return Result<CurrentUserDto>.Failure("El apellido no puede estar vacío.");
            user.LastName = request.LastName.Trim();
        }

        if (request.Phone is not null)
        {
            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, request.Phone);
        }

        if (request.TelegramChatId is not null)
            user.TelegramChatId = string.IsNullOrWhiteSpace(request.TelegramChatId) ? null : request.TelegramChatId.Trim();

        await db.SaveChangesAsync(ct);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return Result<CurrentUserDto>.Success(ToDto(user, roles));
    }

    // Exige la contraseña actual (a diferencia de UpdateOwnProfileAsync): a diferencia del resto
    // del perfil, la contraseña es lo único que por sí sola habilita tomar la cuenta, así que no
    // alcanza con estar logueado — hay que probar que se la sigue sabiendo.
    public async Task<Result<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Result<bool>.Failure("Usuario no encontrado.");

        if (!passwordHasher.Verify(user, user.PasswordHash, request.CurrentPassword))
            return Result<bool>.Failure("La contraseña actual es incorrecta.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return Result<bool>.Failure("La contraseña nueva debe tener al menos 8 caracteres.");

        user.PasswordHash = passwordHasher.Hash(user, request.NewPassword);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    // Autoservicio: el propio usuario logueado genera su link, sin necesidad de una pantalla de
    // gestión de usuarios (que hoy no existe). Un código nuevo pisa cualquier link pendiente
    // anterior de ese usuario.
    public async Task<Result<TelegramLinkDto>> GenerateTelegramLinkAsync(int userId, CancellationToken ct = default)
    {
        var botUsername = telegramNotifier.BotUsername;
        if (string.IsNullOrWhiteSpace(botUsername))
            return Result<TelegramLinkDto>.Failure("Telegram no está configurado todavía.");

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Result<TelegramLinkDto>.Failure("Usuario no encontrado.");

        var code = RandomNumberGenerator.GetHexString(32);
        var expiresAt = DateTimeOffset.UtcNow.Add(TelegramLinkCodeLifetime);

        user.TelegramLinkCode = code;
        user.TelegramLinkCodeExpiresAt = expiresAt;
        await db.SaveChangesAsync(ct);

        return Result<TelegramLinkDto>.Success(new TelegramLinkDto($"https://t.me/{botUsername}?start={code}", expiresAt));
    }

    // Llamado desde el webhook de Telegram (sin sesión de usuario, por eso IgnoreQueryFilters y
    // busca por código en vez de por organización). Nunca lanza: el caller (WebhooksController)
    // decide qué responderle a Telegram según el Result.
    public async Task<Result<bool>> CompleteTelegramLinkAsync(string code, string chatId, CancellationToken ct = default)
    {
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TelegramLinkCode == code && u.TelegramLinkCodeExpiresAt > DateTimeOffset.UtcNow, ct);

        if (user is null)
            return Result<bool>.Failure("Link inválido o expirado.");

        user.TelegramChatId = chatId;
        user.TelegramLinkCode = null;
        user.TelegramLinkCodeExpiresAt = null;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
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
        new(user.Id, user.FirstName, user.LastName, user.Email, user.Phone, user.TelegramChatId,
            user.OrganizationId, roles, user.TelegramChatId is not null);

    private static string? ValidateRegister(RegisterOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return "El nombre de la organización es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.OwnerFirstName))
            return "El nombre del propietario es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.OwnerEmail) || !request.OwnerEmail.Contains('@'))
            return "El email no es válido.";
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        return null;
    }
}
