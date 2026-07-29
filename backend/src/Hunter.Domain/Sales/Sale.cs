using Hunter.Domain.Crm;
using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Sales;

public class Sale
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int? CampaignId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int SellerId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";

    public decimal? Margin { get; set; }
    public string? ProductCategory { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Won;

    public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
