namespace Hunter.Domain.Identity;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public static class RoleNames
{
    public const string Owner = "OWNER";
    public const string Admin = "ADMIN";
    public const string Manager = "MANAGER";
    public const string Seller = "SELLER";
}

public static class RoleIds
{
    public const int Owner = 1;
    public const int Admin = 2;
    public const int Manager = 3;
    public const int Seller = 4;
}
