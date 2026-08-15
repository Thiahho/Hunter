using Hunter.Application.Crm;
using Hunter.Application.Crm.Contracts;
using Hunter.Domain.Crm;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Alto #3: AssignAsync asignaba un Lead a cualquier UserId sin
// verificar que existiera o estuviera activo — el lead quedaba "asignado" a nadie que lo fuera a
// atender.
public class LeadAssignAsyncTests
{
    private static async Task<(int orgId, int leadId)> SeedLeadAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        var lead = new Lead { OrganizationId = org.Id, ProspectId = prospect.Id };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return (org.Id, lead.Id);
    }

    private static async Task<int> SeedUserAsync(string dbName, int orgId, bool isActive = true)
    {
        await using var db = TestDb.Create(dbName);
        var user = new User
        {
            OrganizationId = orgId,
            FirstName = "Vendedor",
            LastName = "Uno",
            Email = $"vendedor{Guid.NewGuid():N}@difrani.com",
            PasswordHash = "hash",
            IsActive = isActive
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task AssignAsync_NonExistentUserId_FailsAndLeavesLeadUnassigned()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new LeadService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });

        var result = await service.AssignAsync(leadId, new AssignLeadRequest(999_999));

        Assert.False(result.Succeeded);

        var lead = await db.Leads.FirstAsync(l => l.Id == leadId);
        Assert.Null(lead.AssignedToUserId);
    }

    [Fact]
    public async Task AssignAsync_InactiveUserId_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedLeadAsync(dbName);
        var inactiveUserId = await SeedUserAsync(dbName, orgId, isActive: false);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new LeadService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });

        var result = await service.AssignAsync(leadId, new AssignLeadRequest(inactiveUserId));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AssignAsync_UserFromAnotherOrganization_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedLeadAsync(dbName);

        int otherOrgId;
        await using (var seedDb = TestDb.Create(dbName))
        {
            var otherOrg = new Organization { Name = "Tauro Parts" };
            seedDb.Organizations.Add(otherOrg);
            await seedDb.SaveChangesAsync();
            otherOrgId = otherOrg.Id;
        }
        var otherOrgUserId = await SeedUserAsync(dbName, otherOrgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new LeadService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });

        var result = await service.AssignAsync(leadId, new AssignLeadRequest(otherOrgUserId));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AssignAsync_ValidActiveUserInSameOrganization_Succeeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedLeadAsync(dbName);
        var userId = await SeedUserAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new LeadService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });

        var result = await service.AssignAsync(leadId, new AssignLeadRequest(userId));

        Assert.True(result.Succeeded);

        var lead = await db.Leads.FirstAsync(l => l.Id == leadId);
        Assert.Equal(userId, lead.AssignedToUserId);
    }
}
