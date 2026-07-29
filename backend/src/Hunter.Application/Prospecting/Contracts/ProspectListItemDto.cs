using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ProspectListItemDto(
    int Id,
    string BusinessName,
    ProspectCategory Category,
    string? City,
    string? Province,
    ProspectStatus Status,
    int? CommercialScore,
    OperationalPriority? OperationalPriority,
    string? PrimaryContactValue,
    DateTimeOffset CreatedAt);
