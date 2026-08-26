using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;

namespace Hunter.Tests.Integration;

// DailyProspectingPlanService no escribe nada directo: reparte el pool de localidades en chunks
// de a 5 y llama a ScheduledProspectAutomationService.CreateAsync (el real, contra la misma
// InMemory db) una vez por chunk — estos tests verifican el reparto/escalonado, no vuelven a
// probar la resolución de campaña ni las validaciones por Source (ya cubiertas en
// ScheduledProspectAutomationServiceTests).
public class DailyProspectingPlanServiceTests
{
    private static async Task<int> SeedOrgWithTemplateAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var template = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Bienvenida",
            Content = "Hola {{business_name}}!",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();

        return org.Id;
    }

    private static (ScheduledProspectAutomationService automationService, DailyProspectingPlanService planService) BuildServices(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int orgId)
    {
        var currentUser = new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 };
        var automationService = new ScheduledProspectAutomationService(db, currentUser, null!, null!);
        var planService = new DailyProspectingPlanService(automationService);
        return (automationService, planService);
    }

    private static List<string> Localities(int count) => Enumerable.Range(1, count).Select(i => $"Localidad{i}").ToList();

    [Fact]
    public async Task CreateAsync_TwelveLocalitiesWithApify_CreatesThreeOsmAndThreeApifyBatchesStaggered()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgWithTemplateAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var (_, planService) = BuildServices(db, orgId);

        var startAt = DateTimeOffset.UtcNow.AddHours(1);
        var request = new CreateDailyProspectingPlanRequest(Localities(12), startAt, IntervalMinutes: 20, RadiusKm: 10, IncludeApify: true);

        var result = await planService.CreateAsync(request);

        Assert.True(result.Succeeded);
        var dto = result.Value!;
        Assert.Equal(12, dto.LocalitiesCovered);
        Assert.Equal(6, dto.Automations.Count); // 3 chunks OSM (5+5+2) + 3 chunks Apify (5+5+2)
        Assert.Equal(3 * 300 + 3 * 100, dto.EstimatedCeiling);

        var osmBatches = dto.Automations.Where(a => a.Source == ProspectAutomationSource.OpenStreetMap).ToList();
        var apifyBatches = dto.Automations.Where(a => a.Source == ProspectAutomationSource.Apify).ToList();
        Assert.Equal(3, osmBatches.Count);
        Assert.Equal(3, apifyBatches.Count);
        Assert.Equal(5, osmBatches[0].Localities.Count);
        Assert.Equal(5, osmBatches[1].Localities.Count);
        Assert.Equal(2, osmBatches[2].Localities.Count);

        // Cada corrida (OSM primero, después Apify) quema un horario distinto separado por
        // IntervalMinutes, en el orden en que se crearon.
        var scheduledTimes = dto.Automations.Select(a => a.ScheduledAt).ToList();
        for (var i = 0; i < scheduledTimes.Count; i++)
            Assert.Equal(startAt.AddMinutes(20 * i), scheduledTimes[i]);
    }

    [Fact]
    public async Task CreateAsync_IncludeApifyFalse_CreatesOnlyOsmBatches()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgWithTemplateAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var (_, planService) = BuildServices(db, orgId);

        var request = new CreateDailyProspectingPlanRequest(
            Localities(7), DateTimeOffset.UtcNow.AddHours(1), IntervalMinutes: 20, RadiusKm: 10, IncludeApify: false);

        var result = await planService.CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.All(result.Value!.Automations, a => Assert.Equal(ProspectAutomationSource.OpenStreetMap, a.Source));
        Assert.Equal(2, result.Value.Automations.Count); // 5 + 2
        Assert.Equal(2 * 300, result.Value.EstimatedCeiling);
    }

    [Fact]
    public async Task CreateAsync_NoLocalities_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgWithTemplateAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var (_, planService) = BuildServices(db, orgId);

        var result = await planService.CreateAsync(
            new CreateDailyProspectingPlanRequest([], DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task CreateAsync_StartAtInThePast_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgWithTemplateAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var (_, planService) = BuildServices(db, orgId);

        var result = await planService.CreateAsync(
            new CreateDailyProspectingPlanRequest(Localities(3), DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.False(result.Succeeded);
    }
}
