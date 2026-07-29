namespace Hunter.Domain.Prospecting;

public class ProspectTag
{
    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
