using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record MessageDto(
    int Id,
    int ProspectId,
    string ProspectBusinessName,
    int? CampaignId,
    MessagingChannel Channel,
    string Content,
    MessageStatus Status,
    string? ExternalMessageId,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt);
