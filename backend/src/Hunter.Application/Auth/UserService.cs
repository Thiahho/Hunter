using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Domain.Identity;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Auth;

public class UserService(IHunterDbContext db, IPasswordHasher passwordHasher, ICurrentUserService currentUser) : IUserService
{
    private static readonly Dictionary<string, int> InvitableRoles = new()
    {
        [RoleNames.Admin] = RoleIds.Admin,
        [RoleNames.Manager] = RoleIds.Manager,
        [RoleNames.Seller] = RoleIds.Seller
    };

    public async Task<Result<IReadOnlyCollection<UserDto>>> ListAsync(int organizationId, CancellationToken ct = default)
    {
        var users = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.OrganizationId == organizationId)
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

        var dtos = users.Select(u => ToDto(u, u.UserRoles.Select(ur => ur.Role.Name).ToList())).ToList();
        return Result<IReadOnlyCollection<UserDto>>.Success(dtos);
    }

    public async Task<Result<UserDto>> CreateAsync(int organizationId, CreateUserRequest request, CancellationToken ct = default)
    {
        // Un Admin podía crear otros Admin (mismo nivel de acceso que el suyo): si esa cuenta se
        // compromete, el atacante se multiplica pares con su mismo poder en vez de quedar
        // contenido. Crear un Admin ahora queda reservado al Owner (auditoria.md, hallazgo Info).
        if (string.Equals(request.Role.Trim(), RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            && !currentUser.Roles.Contains(RoleNames.Owner))
            return Result<UserDto>.Failure("Solo el Owner puede crear usuarios con rol Admin.");

        var validationError = await ValidateAsync(organizationId, request, ct);
        if (validationError is not null)
            return Result<UserDto>.Failure(validationError);

        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName?.Trim() ?? string.Empty,
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, request.Phone),
            // A diferencia de Phone, no es un contacto telefónico: es el chat_id numérico que
            // devuelve @userinfobot, se guarda tal cual (trim, sin normalizador de teléfono).
            TelegramChatId = string.IsNullOrWhiteSpace(request.TelegramChatId) ? null : request.TelegramChatId.Trim(),
            Area = request.Area
        };
        user.PasswordHash = passwordHasher.Hash(user, request.Password);

        var roleId = InvitableRoles[request.Role.Trim().ToUpperInvariant()];
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, User = user, RoleId = roleId });

        await db.SaveChangesAsync(ct);

        return Result<UserDto>.Success(ToDto(user, [request.Role.Trim().ToUpperInvariant()]));
    }

    public async Task<Result<UserDto>> UpdateAsync(int organizationId, int userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .IgnoreQueryFilters()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId, ct);

        if (user is null)
            return Result<UserDto>.Failure("Usuario no encontrado.");

        if (request.Email is not null)
        {
            if (!currentUser.Roles.Contains(RoleNames.Owner))
                return Result<UserDto>.Failure("Solo el Owner puede cambiar el email de un usuario.");

            var email = request.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return Result<UserDto>.Failure("El email no es válido.");

            var emailTaken = await db.Users.IgnoreQueryFilters()
                .AnyAsync(u => u.OrganizationId == organizationId && u.Id != userId && u.Email == email, ct);
            if (emailTaken)
                return Result<UserDto>.Failure("Ya existe un usuario con ese email en la organización.");

            user.Email = email;
        }

        if (request.Phone is not null)
        {
            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, request.Phone);
        }

        if (request.TelegramChatId is not null)
            user.TelegramChatId = string.IsNullOrWhiteSpace(request.TelegramChatId) ? null : request.TelegramChatId.Trim();

        if (request.Area is not null)
            user.Area = request.Area.Value;

        if (request.IsActive is not null)
            user.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync(ct);

        return Result<UserDto>.Success(ToDto(user, user.UserRoles.Select(ur => ur.Role.Name).ToList()));
    }

    private async Task<string?> ValidateAsync(int organizationId, CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
            return "El nombre es obligatorio.";
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return "El email no es válido.";
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return "La contraseña debe tener al menos 8 caracteres.";
        if (!InvitableRoles.ContainsKey(request.Role.Trim().ToUpperInvariant()))
            return $"Rol inválido. Valores permitidos: {string.Join(", ", InvitableRoles.Keys)}.";

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await db.Users.AnyAsync(u => u.OrganizationId == organizationId && u.Email == email, ct);
        if (emailTaken)
            return "Ya existe un usuario con ese email en la organización.";

        return null;
    }

    private static UserDto ToDto(User user, IReadOnlyCollection<string> roles) =>
        new(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, roles, user.Phone, user.TelegramChatId, user.Area);
}
