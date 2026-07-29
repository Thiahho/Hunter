namespace Hunter.Domain.Crm;

public class LeadActivity
{
    public int Id { get; set; }

    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    public int UserId { get; set; }

    public LeadActivityType Type { get; set; }
    public string Description { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
