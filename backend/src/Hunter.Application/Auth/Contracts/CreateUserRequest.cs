using Hunter.Domain.Identity;

namespace Hunter.Application.Auth.Contracts;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role,
    string? Phone = null,
    UserArea Area = UserArea.Unassigned);
