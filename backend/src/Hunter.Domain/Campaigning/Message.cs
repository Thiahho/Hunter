using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Campaigning;

public class Message
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int? CampaignId { get; set; }
    public int? CampaignRecipientId { get; set; }
    public int? TemplateId { get; set; }

    public MessagingChannel Channel { get; set; }
    public string Provider { get; set; } = null!;
    public string Content { get; set; } = null!;

    public string? ExternalMessageId { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureReason { get; set; }

    public decimal? Cost { get; set; }
    public string? Currency { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
