using Hunter.Domain.Common;
using Hunter.Domain.Organizations;

namespace Hunter.Domain.Identity;

public class User : Entity
{
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public UserArea Area { get; set; } = UserArea.Unassigned;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
