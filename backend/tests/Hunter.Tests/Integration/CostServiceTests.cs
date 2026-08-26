using Hunter.Application.Finance;
using Hunter.Application.Finance.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Finance;
using Hunter.Domain.Organizations;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Medio (validación de dinero incompleta en CostService):
// Currency sin validar y CampaignId sin verificar que exista/pertenezca a la organización.
public class CostServiceTests
{
    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static CostService CreateService(Hunter.Infrastructure.Persistence.HunterDbContext db, int orgId, int userId = 1) =>
        new(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = userId });

    [Fact]
    public async Task CreateAsync_InvalidCurrency_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(new CreateCostRequest(CostType.Messaging, "Meta", 100, "US"));

        Assert.False(result.Succeeded);
        Assert.Empty(db.Costs);
    }

    [Fact]
    public async Task CreateAsync_NonExistentCampaignId_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(new CreateCostRequest(CostType.Messaging, "Meta", 100, "ARS", CampaignId: 999_999));

        Assert.False(result.Succeeded);
        Assert.Empty(db.Costs);
    }

    [Fact]
    public async Task CreateAsync_CampaignFromAnotherOrganization_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        int otherCampaignId;
        await using (var seedDb = TestDb.Create(dbName))
        {
            var otherOrg = new Organization { Name = "Tauro Parts" };
            seedDb.Organizations.Add(otherOrg);
            await seedDb.SaveChangesAsync();

            var template = new MessageTemplate
            {
                OrganizationId = otherOrg.Id,
                Name = "Plantilla",
                Content = "Hola",
                Channel = MessagingChannel.Whatsapp
            };
            seedDb.MessageTemplates.Add(template);
            await seedDb.SaveChangesAsync();

            var campaign = new Campaign
            {
                OrganizationId = otherOrg.Id,
                Name = "Campaña de otra org",
                Channel = MessagingChannel.Whatsapp,
                MessageTemplateId = template.Id
            };
            seedDb.Campaigns.Add(campaign);
            await seedDb.SaveChangesAsync();
            otherCampaignId = campaign.Id;
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(new CreateCostRequest(CostType.Messaging, "Meta", 100, "ARS", CampaignId: otherCampaignId));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_ValidRequestWithoutCampaign_NormalizesCurrencyAndSucceeds()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(new CreateCostRequest(CostType.Messaging, "Meta", 100, "ars"));

        Assert.True(result.Succeeded);
        var cost = await db.Costs.SingleAsync();
        Assert.Equal("ARS", cost.Currency);
    }
}
