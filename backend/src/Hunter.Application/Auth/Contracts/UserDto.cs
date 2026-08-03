using Hunter.Domain.Identity;

namespace Hunter.Application.Auth.Contracts;

public record UserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    string? Phone,
    UserArea Area);
