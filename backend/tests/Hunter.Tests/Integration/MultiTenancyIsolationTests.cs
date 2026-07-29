using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Caso crítico obligatorio de doc 07 (Epic14) / doc 10 (Sprint 9):
// "Usuario A NO puede acceder a datos de Organización B."
public class MultiTenancyIsolationTests
{
    [Fact]
    public async Task Organization_Cannot_See_Prospects_From_Another_Organization()
    {
        var dbName = TestDb.NewDbName();

        Organization orgA, orgB;
        await using (var seedDb = TestDb.Create(dbName))
        {
            orgA = new Organization { Name = "Difrani" };
            orgB = new Organization { Name = "Tauro Parts" };
            seedDb.Organizations.AddRange(orgA, orgB);
            await seedDb.SaveChangesAsync();
        }

        Prospect prospectA;
        await using (var dbA = TestDb.Create(dbName, organizationId: orgA.Id))
        {
            prospectA = new Prospect { OrganizationId = orgA.Id, BusinessName = "Repuestos Oeste" };
            dbA.Prospects.Add(prospectA);
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = TestDb.Create(dbName, organizationId: orgB.Id))
        {
            var visibleToOrgB = await dbB.Prospects.ToListAsync();
            Assert.Empty(visibleToOrgB);

            var directAccess = await dbB.Prospects.FirstOrDefaultAsync(p => p.Id == prospectA.Id);
            Assert.Null(directAccess);
        }

        await using (var dbA2 = TestDb.Create(dbName, organizationId: orgA.Id))
        {
            var visibleToOrgA = await dbA2.Prospects.ToListAsync();
            Assert.Single(visibleToOrgA);
            Assert.Equal(prospectA.Id, visibleToOrgA[0].Id);
        }
    }

    [Fact]
    public async Task Unauthenticated_Context_Sees_Nothing_By_Default()
    {
        var dbName = TestDb.NewDbName();

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();

            seedDb.Prospects.Add(new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste" });
            await seedDb.SaveChangesAsync();
        }

        // Sin OrganizationId (contexto anónimo / diseño): el filtro global es "deny by default",
        // no "allow all" (doc 06, sección 37).
        await using var anonymousDb = TestDb.Create(dbName, organizationId: null);
        var result = await anonymousDb.Prospects.ToListAsync();

        Assert.Empty(result);
    }
}
