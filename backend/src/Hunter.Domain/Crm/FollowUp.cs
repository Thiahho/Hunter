namespace Hunter.Domain.Crm;

public class FollowUp
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int UserId { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;
    public string? Notes { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
