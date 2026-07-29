namespace Hunter.Application.Auth.Contracts;

public record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role);
