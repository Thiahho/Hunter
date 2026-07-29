namespace Hunter.Application.Auth.Contracts;

public record RegisterOrganizationRequest(
    string OrganizationName,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    string Password);
