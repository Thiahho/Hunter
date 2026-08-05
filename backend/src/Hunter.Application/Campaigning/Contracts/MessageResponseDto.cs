using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record MessageResponseDto(
    int Id,
    int ProspectId,
    string ProspectBusinessName,
    int? CampaignId,
    int? MessageId,
    string Content,
    DateTimeOffset ReceivedAt,
    IntentClassification Classification,
    decimal Confidence,
    string? ButtonPayload,
    DateTimeOffset? ProcessedAt);
