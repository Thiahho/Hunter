namespace Hunter.Domain.Finance;

public class Cost
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }
    public int? CampaignId { get; set; }

    public CostType Type { get; set; }
    public string Provider { get; set; } = null!;
    public string? ReferenceId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";

    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
