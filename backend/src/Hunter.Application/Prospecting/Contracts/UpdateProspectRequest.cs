using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record UpdateProspectRequest(
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
    ProspectStatus Status);
