using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ProspectListItemDto(
    int Id,
    string BusinessName,
    ProspectCategory Category,
    string? Address,
    string? City,
    string? Province,
    double? Latitude,
    double? Longitude,
    ProspectStatus Status,
    int? CommercialScore,
    OperationalPriority? OperationalPriority,
    string? PrimaryContactValue,
    DateTimeOffset CreatedAt);
