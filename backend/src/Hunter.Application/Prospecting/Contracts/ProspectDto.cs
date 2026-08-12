using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ProspectContactDto(int Id, ProspectContactChannel Channel, string Value, bool IsPrimary, bool IsVerified);

public record AddProspectContactRequest(ProspectContactChannel Channel, string Value, bool IsPrimary);

public record UpdateProspectContactRequest(ProspectContactChannel Channel, string Value, bool IsPrimary);

public record ProspectSourceDto(int Id, ProspectSourceType SourceType, string? ExternalId, string? SourceUrl, DateTimeOffset CollectedAt);

public record ProspectDto(
    int Id,
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
    double? Latitude,
    double? Longitude,
    string? Website,
    int? CommercialScore,
    OperationalPriority? OperationalPriority,
    ProspectStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastContactedAt,
    DateTimeOffset? LastViewedAt,
    IReadOnlyCollection<ProspectContactDto> Contacts,
    IReadOnlyCollection<ProspectSourceDto> Sources,
    IReadOnlyCollection<string> Tags);
