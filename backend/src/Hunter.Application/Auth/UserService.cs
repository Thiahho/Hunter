using Hunter.Application.Auth.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Identity;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Auth;

public class UserService(IHunterDbContext db, IPasswordHasher passwordHasher) : IUserService
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
        var validationError = await ValidateAsync(organizationId, request, ct);
        if (validationError is not null)
            return Result<UserDto>.Failure(validationError);

        var user = new User
        {
            OrganizationId = organizationId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant()
        };
        user.PasswordHash = passwordHasher.Hash(user, request.Password);

        var roleId = InvitableRoles[request.Role.Trim().ToUpperInvariant()];
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { UserId = user.Id, User = user, RoleId = roleId });

        await db.SaveChangesAsync(ct);

        return Result<UserDto>.Success(ToDto(user, [request.Role.Trim().ToUpperInvariant()]));
    }

    private async Task<string?> ValidateAsync(int organizationId, CreateUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return "El nombre y apellido son obligatorios.";
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
        new(user.Id, user.FirstName, user.LastName, user.Email, user.IsActive, roles);
}
