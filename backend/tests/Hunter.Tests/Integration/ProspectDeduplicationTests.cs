using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;

namespace Hunter.Tests.Integration;

// Caso crítico obligatorio de doc 07 (Epic14) / doc 10 (Sprint 9):
// "Prospecto duplicado NO genera nuevo registro."
public class ProspectDeduplicationTests
{
    private static CreateProspectRequest BuildRequest(string businessName, string whatsapp) => new(
        businessName,
        null,
        ProspectCategory.Workshop,
        BusinessSize.Small,
        RecurrencePotential.Medium,
        null, null, null, null, null, null,
        [new ContactInput(ProspectContactChannel.Whatsapp, whatsapp, true)],
        ProspectSourceType.Manual);

    [Fact]
    public async Task Creating_Prospect_With_Same_Contact_Different_Format_Is_Rejected()
    {
        var dbName = TestDb.NewDbName();
        int orgId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();
            orgId = org.Id;
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var finder = new ProspectDuplicateFinder(db);
        var service = new ProspectService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, finder);

        var first = await service.CreateAsync(BuildRequest("Repuestos Oeste", "011 15 1234-5678"));
        Assert.True(first.Succeeded);

        var second = await service.CreateAsync(BuildRequest("Repuestos Oeste Autopartes", "0111512345678"));

        Assert.False(second.Succeeded);
        Assert.Contains(first.Value!.Id.ToString(), second.Error);
    }

    [Fact]
    public async Task Creating_Prospect_With_Same_Contact_In_Different_Organization_Is_Allowed()
    {
        var dbName = TestDb.NewDbName();
        int orgAId, orgBId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var orgA = new Organization { Name = "Difrani" };
            var orgB = new Organization { Name = "Tauro Parts" };
            seedDb.Organizations.AddRange(orgA, orgB);
            await seedDb.SaveChangesAsync();
            orgAId = orgA.Id;
            orgBId = orgB.Id;
        }

        await using (var dbA = TestDb.Create(dbName, organizationId: orgAId, userId: 1))
        {
            var serviceA = new ProspectService(dbA, new FakeCurrentUserService { OrganizationId = orgAId, UserId = 1 }, new ProspectDuplicateFinder(dbA));
            var resultA = await serviceA.CreateAsync(BuildRequest("Repuestos Oeste", "5491112345678"));
            Assert.True(resultA.Succeeded);
        }

        await using (var dbB = TestDb.Create(dbName, organizationId: orgBId, userId: 2))
        {
            var serviceB = new ProspectService(dbB, new FakeCurrentUserService { OrganizationId = orgBId, UserId = 2 }, new ProspectDuplicateFinder(dbB));
            var resultB = await serviceB.CreateAsync(BuildRequest("Cliente de Tauro", "5491112345678"));

            // El mismo contacto puede existir en dos organizaciones distintas (doc 05, regla 4:
            // la dedup es "dentro de una organización", no global).
            Assert.True(resultB.Succeeded);
        }
    }
}
