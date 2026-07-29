namespace Hunter.Domain.Compliance;

public class Suppression
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public string Contact { get; set; } = null!;
    public SuppressionContactType ContactType { get; set; }

    public SuppressionReason Reason { get; set; }
    public string? Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
