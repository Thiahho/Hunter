using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record CreateProspectRequest(
    string BusinessName,
    string? ContactName,
    ProspectCategory Category,
    BusinessSize BusinessSize,
    RecurrencePotential RecurrencePotential,
    string? Address,
    string? City,
    string? Province,
    string? Country,
    string? PostalCode,
    string? Website,
    IReadOnlyCollection<ContactInput> Contacts,
    ProspectSourceType SourceType,
    string? SourceExternalId = null,
    string? SourceUrl = null,
    IReadOnlyCollection<string>? Tags = null);
