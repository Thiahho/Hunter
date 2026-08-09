namespace Hunter.Application.Auth.Contracts;

public record RegisterOrganizationRequest(
    string OrganizationName,
    string OwnerFirstName,
    string OwnerEmail,
    string Password,
    string? OwnerLastName = null);
