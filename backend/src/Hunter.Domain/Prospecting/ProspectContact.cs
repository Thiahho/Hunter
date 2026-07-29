using Hunter.Domain.Common;

namespace Hunter.Domain.Prospecting;

public class ProspectContact : Entity
{
    public int OrganizationId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public ProspectContactChannel Channel { get; set; }
    public string Value { get; set; } = null!;

    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
}
