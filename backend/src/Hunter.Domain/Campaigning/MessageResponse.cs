using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Campaigning;

public class MessageResponse
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int? CampaignId { get; set; }
    public int? MessageId { get; set; }

    public string Content { get; set; } = null!;
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

    public IntentClassification Classification { get; set; }
    public decimal Confidence { get; set; }
    public string? AiModel { get; set; }
    public string? AiPromptVersion { get; set; }

    public string? ExternalInboundId { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
