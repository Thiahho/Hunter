using Hunter.Application.Crm;
using Hunter.Domain.Crm;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;

namespace Hunter.Tests.Unit;

// Regresión de auditoria.md hallazgo Medio "el feature de backlog no tiene ningún test": esta es
// la lógica que decide si se pausa el envío de campañas nuevas cuando ya hay demasiados leads
// abiertos sin trabajar (caso Difrani, ver OpenLeadBacklogGuard).
public class OpenLeadBacklogGuardTests
{
    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static async Task SeedLeadsAsync(string dbName, int orgId, int count, LeadStatus status = LeadStatus.New)
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        for (var i = 0; i < count; i++)
        {
            var prospect = new Prospect { OrganizationId = orgId, BusinessName = $"Prospecto {i}" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();

            db.Leads.Add(new Lead { OrganizationId = orgId, ProspectId = prospect.Id, Status = status });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task EvaluateAsync_NoLeads_BacklogZero_DefaultThreshold()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var (backlog, threshold) = await OpenLeadBacklogGuard.EvaluateAsync(db, orgId);

        Assert.Equal(0, backlog);
        Assert.Equal(OpenLeadBacklogGuard.DefaultThreshold, threshold);
    }

    [Fact]
    public async Task EvaluateAsync_OnlyCountsNewAndInProgress_NotWonOrLost()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await SeedLeadsAsync(dbName, orgId, 3, LeadStatus.New);
        await SeedLeadsAsync(dbName, orgId, 2, LeadStatus.InProgress);
        await SeedLeadsAsync(dbName, orgId, 10, LeadStatus.Won);
        await SeedLeadsAsync(dbName, orgId, 10, LeadStatus.Lost);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var (backlog, _) = await OpenLeadBacklogGuard.EvaluateAsync(db, orgId);

        Assert.Equal(5, backlog);
    }

    [Fact]
    public async Task EvaluateAsync_NoOrganizationSetting_UsesDefaultThreshold()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedLeadsAsync(dbName, orgId, OpenLeadBacklogGuard.DefaultThreshold + 1, LeadStatus.New);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var (backlog, threshold) = await OpenLeadBacklogGuard.EvaluateAsync(db, orgId);

        Assert.Equal(OpenLeadBacklogGuard.DefaultThreshold, threshold);
        Assert.True(backlog > threshold);
    }

    [Fact]
    public async Task EvaluateAsync_CustomOrganizationSetting_OverridesDefault()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedLeadsAsync(dbName, orgId, 3, LeadStatus.New);

        await using (var db = TestDb.Create(dbName, organizationId: orgId))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = orgId,
                Key = OrganizationSettingsKeys.OpenLeadBacklogThreshold,
                Value = "2"
            });
            await db.SaveChangesAsync();
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var (backlog, threshold) = await OpenLeadBacklogGuard.EvaluateAsync(assertDb, orgId);

        Assert.Equal(2, threshold);
        Assert.Equal(3, backlog);
        Assert.True(backlog > threshold); // con el umbral bajado a 2, 3 leads ya superan el límite
    }

    [Theory]
    [InlineData("0")] // umbral inválido (<=0): cae al default en vez de bloquear todo
    [InlineData("-5")]
    [InlineData("no-es-un-numero")]
    public async Task EvaluateAsync_InvalidOrganizationSetting_FallsBackToDefault(string invalidValue)
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using (var db = TestDb.Create(dbName, organizationId: orgId))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = orgId,
                Key = OrganizationSettingsKeys.OpenLeadBacklogThreshold,
                Value = invalidValue
            });
            await db.SaveChangesAsync();
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var (_, threshold) = await OpenLeadBacklogGuard.EvaluateAsync(assertDb, orgId);

        Assert.Equal(OpenLeadBacklogGuard.DefaultThreshold, threshold);
    }

    [Fact]
    public async Task EvaluateAsync_IgnoresLeadsAndSettingsFromOtherOrganizations()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        int otherOrgId;
        await using (var seedDb = TestDb.Create(dbName))
        {
            var otherOrg = new Organization { Name = "Tauro Parts" };
            seedDb.Organizations.Add(otherOrg);
            await seedDb.SaveChangesAsync();
            otherOrgId = otherOrg.Id;
        }

        await SeedLeadsAsync(dbName, otherOrgId, 50, LeadStatus.New);
        await using (var db = TestDb.Create(dbName, organizationId: otherOrgId))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = otherOrgId,
                Key = OrganizationSettingsKeys.OpenLeadBacklogThreshold,
                Value = "1"
            });
            await db.SaveChangesAsync();
        }

        await SeedLeadsAsync(dbName, orgId, 2, LeadStatus.New);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var (backlog, threshold) = await OpenLeadBacklogGuard.EvaluateAsync(assertDb, orgId);

        Assert.Equal(2, backlog);
        Assert.Equal(OpenLeadBacklogGuard.DefaultThreshold, threshold);
    }
}
