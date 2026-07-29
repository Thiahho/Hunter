namespace Hunter.Domain.Prospecting;

public class ProspectSource
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public ProspectSourceType SourceType { get; set; }
    public string? ExternalId { get; set; }
    public string? SourceUrl { get; set; }
    public DateTimeOffset CollectedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
