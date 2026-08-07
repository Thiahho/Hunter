using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record ScheduleMessageRequest(int MessageTemplateId, DateTimeOffset ScheduledAt);

public record ScheduledMessageDto(
    int Id,
    int ProspectId,
    int MessageTemplateId,
    string MessageTemplateName,
    DateTimeOffset ScheduledAt,
    ScheduledMessageStatus Status,
    DateTimeOffset? RunAt,
    int? MessageId,
    string? FailureReason,
    DateTimeOffset CreatedAt);
