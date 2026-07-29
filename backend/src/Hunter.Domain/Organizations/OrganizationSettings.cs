using Hunter.Domain.Common;

namespace Hunter.Domain.Organizations;

public class OrganizationSettings : Entity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}
