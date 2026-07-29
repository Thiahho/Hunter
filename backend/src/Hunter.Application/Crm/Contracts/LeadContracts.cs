using Hunter.Domain.Crm;

namespace Hunter.Application.Crm.Contracts;

public record LeadActivityDto(int Id, LeadActivityType Type, string Description, int UserId, DateTimeOffset CreatedAt);

public record FollowUpDto(int Id, DateTimeOffset ScheduledAt, FollowUpStatus Status, string? Notes, DateTimeOffset? CompletedAt);

public record LeadDto(
    int Id,
    int ProspectId,
    string ProspectBusinessName,
    int? CampaignId,
    int? AssignedToUserId,
    LeadStatus Status,
    LeadPriority Priority,
    LostReason? LostReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? FirstResponseAt,
    DateTimeOffset? LastActivityAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyCollection<LeadActivityDto> Activities,
    IReadOnlyCollection<FollowUpDto> FollowUps);

public record LeadListItemDto(
    int Id,
    int ProspectId,
    string ProspectBusinessName,
    LeadStatus Status,
    LeadPriority Priority,
    int? AssignedToUserId,
    DateTimeOffset CreatedAt);

public record LeadQuery(
    LeadStatus? Status = null,
    LeadPriority? Priority = null,
    int? AssignedUserId = null,
    int? CampaignId = null,
    int Page = 1,
    int PageSize = 50);

public record AssignLeadRequest(int? UserId);

public record CreateLeadActivityRequest(LeadActivityType Type, string Description);

public record CreateFollowUpRequest(DateTimeOffset ScheduledAt, string? Notes);

public record MarkWonRequest(decimal Amount, string Currency, decimal? Margin, string? ProductCategory);

public record MarkLostRequest(LostReason LostReason, string? Notes);
