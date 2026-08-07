using Hunter.Domain.Campaigning;
using Hunter.Domain.Prospecting;

namespace Hunter.Application.Campaigning.Contracts;

public record CampaignDto(
    int Id,
    string Name,
    string? Description,
    CampaignStatus Status,
    MessagingChannel Channel,
    int MessageTemplateId,
    string MessageTemplateName,
    int MaxMessages,
    int MessagesPerMinute,
    int MessagesPerHour,
    int MessagesPerDay,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int RecipientsCount,
    int SentCount,
    int RespondedCount,
    DateTimeOffset CreatedAt);

public record CampaignListItemDto(
    int Id,
    string Name,
    CampaignStatus Status,
    MessagingChannel Channel,
    string MessageTemplateName,
    int MessagesPerMinute,
    int RecipientsCount,
    int SentCount,
    DateTimeOffset CreatedAt);

public record CreateCampaignRequest(
    string Name,
    string? Description,
    MessagingChannel Channel,
    int MessageTemplateId,
    int MaxMessages = 1000,
    int MessagesPerMinute = 5,
    int MessagesPerHour = 100,
    int MessagesPerDay = 1000);

public record AddRecipientsRequest(IReadOnlyCollection<int> ProspectIds);

public record AddRecipientsFromSegmentRequest(
    ProspectCategory? Category = null,
    string? City = null,
    string? Province = null,
    BusinessSize? BusinessSize = null,
    string? Tag = null);

public record AddRecipientsResultDto(int Added, int AlreadyInCampaign, int WithoutValidContact, int Suppressed);

public record ProcessQueueResultDto(int Processed, int Sent, int Failed, int Suppressed);

public record KillSwitchRequest(bool Enabled, string? Reason);

public record CampaignRecipientDto(
    int Id,
    int CampaignId,
    string CampaignName,
    int ProspectId,
    string ProspectBusinessName,
    CampaignRecipientStatus Status,
    int Attempts,
    DateTimeOffset? LastAttemptAt,
    string? LastMessageFailureReason,
    DateTimeOffset? LastMessageFailedAt,
    DateTimeOffset CreatedAt);

public record RetryRecipientsRequest(IReadOnlyCollection<int> RecipientIds);

public record RetryRecipientsResultDto(int Retried, int Skipped);
