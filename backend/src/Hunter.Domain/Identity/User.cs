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
    public string? TelegramChatId { get; set; }

    // Código de un solo uso para el flujo de vinculación self-service (deep link t.me/<bot>?start=<code>).
    // Como mucho hay un link pendiente por usuario: uno nuevo pisa al anterior, no hace falta tabla aparte.
    public string? TelegramLinkCode { get; set; }
    public DateTimeOffset? TelegramLinkCodeExpiresAt { get; set; }
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public UserArea Area { get; set; } = UserArea.Unassigned;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
