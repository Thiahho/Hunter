using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Campaigning;

public class CampaignRecipient
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int CampaignId { get; set; }
    public Campaign Campaign { get; set; } = null!;

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public CampaignRecipientStatus Status { get; set; } = CampaignRecipientStatus.Pending;

    public int Attempts { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }

    public int? FirstMessageId { get; set; }
    public int? LastMessageId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
