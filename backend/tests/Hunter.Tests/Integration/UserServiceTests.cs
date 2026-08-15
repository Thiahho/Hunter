using Hunter.Application.Auth;
using Hunter.Application.Auth.Contracts;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Info "un Admin puede crear otros Admin": un Admin
// comprometido podía multiplicarse pares con su mismo nivel de acceso. Crear un usuario con rol
// Admin ahora queda reservado al Owner.
public class UserServiceTests
{
    // El hash real no importa para estos tests, solo que CreateAsync no explote al llamarlo.
    private class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(User user, string password) => "hashed";
        public bool Verify(User user, string hash, string password) => true;
    }

    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static UserService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int orgId, IReadOnlyCollection<string> callerRoles) =>
        new(db, new StubPasswordHasher(), new FakeCurrentUserService { OrganizationId = orgId, UserId = 1, Roles = callerRoles });

    private static CreateUserRequest BuildRequest(string role) =>
        new(FirstName: "Nuevo", Email: $"nuevo{Guid.NewGuid():N}@difrani.com", Password: "password123", Role: role);

    [Fact]
    public async Task CreateAsync_AdminRole_ByOwner_Succeeds()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId, [RoleNames.Owner]);

        var result = await service.CreateAsync(orgId, BuildRequest(RoleNames.Admin));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_AdminRole_ByAdmin_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId, [RoleNames.Admin]);

        var result = await service.CreateAsync(orgId, BuildRequest(RoleNames.Admin));

        Assert.False(result.Succeeded);
        Assert.Empty(await db.Users.ToListAsync());
    }

    [Theory]
    [InlineData(RoleNames.Manager)]
    [InlineData(RoleNames.Seller)]
    public async Task CreateAsync_NonAdminRole_ByAdmin_Succeeds(string role)
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId, [RoleNames.Admin]);

        var result = await service.CreateAsync(orgId, BuildRequest(role));

        Assert.True(result.Succeeded);
    }
}
