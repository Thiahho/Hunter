namespace Hunter.Application.Auth.Contracts;

public record CurrentUserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    int OrganizationId,
    IReadOnlyCollection<string> Roles);
