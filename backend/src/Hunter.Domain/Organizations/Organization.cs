using Hunter.Domain.Common;
using Hunter.Domain.Identity;

namespace Hunter.Domain.Organizations;

public class Organization : Entity
{
    public string Name { get; set; } = null!;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string Timezone { get; set; } = "America/Argentina/Buenos_Aires";
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<OrganizationSettings> Settings { get; set; } = new List<OrganizationSettings>();
}
