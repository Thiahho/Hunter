using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record DailyProspectDto(
    int ProspectId,
    string BusinessName,
    string? Province,
    string? City,
    string? Phone,
    DateTimeOffset CreatedAt,
    bool Sent,
    int Attempts,
    DateTimeOffset? LastAttemptAt,
    MessageStatus? LastStatus);

public record FailedContactDto(
    int ProspectId,
    string BusinessName,
    string? Province,
    string? City,
    string? Phone,
    string? FailureReason,
    DateTimeOffset? FailedAt);
