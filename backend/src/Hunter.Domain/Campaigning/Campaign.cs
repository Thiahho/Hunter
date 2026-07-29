using Hunter.Domain.Common;

namespace Hunter.Domain.Campaigning;

public class Campaign : Entity
{
    public int OrganizationId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public MessagingChannel Channel { get; set; }

    public int MessageTemplateId { get; set; }
    public MessageTemplate MessageTemplate { get; set; } = null!;

    public int MaxMessages { get; set; } = 1000;
    public int MessagesPerMinute { get; set; } = 5;
    public int MessagesPerHour { get; set; } = 100;
    public int MessagesPerDay { get; set; } = 1000;

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public int? CreatedBy { get; set; }

    public ICollection<CampaignRecipient> Recipients { get; set; } = new List<CampaignRecipient>();
}
