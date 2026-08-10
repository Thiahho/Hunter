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
    MessageStatus? LastStatus,
    // Id a pasar a POST /campaigns/recipients/retry (si IsCampaignRecipient) o a
    // POST /messages/{id}/retry (si no) para reintentar el último mensaje de este prospecto.
    // Null si todavía no se le mandó ningún mensaje.
    int? RetryTargetId,
    bool IsCampaignRecipient,
    int? CampaignId);

public record FailedContactDto(
    int ProspectId,
    string BusinessName,
    string? Province,
    string? City,
    string? Phone,
    string? FailureReason,
    DateTimeOffset? FailedAt);
