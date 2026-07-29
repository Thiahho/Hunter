namespace Hunter.Domain.Prospecting;

public class Tag
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public string Name { get; set; } = null!;
    public string? Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ProspectTag> ProspectTags { get; set; } = new List<ProspectTag>();
}
