using Hunter.Application.Campaigning;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Cubre solo CreateAsync/CancelAsync (validaciones + persistencia): RunAsync orquesta
// IImportService + ICampaignService contra OpenStreetMap/WhatsApp reales, que no tiene sentido
// fakear acá — esos dos servicios (ImportService.ImportFromOpenStreetMapAsync/ConfirmAsync,
// CampaignService.AddRecipientsAsync/StartAsync/ProcessQueueAsync) ya están cubiertos por sus
// propios tests. Por eso a IImportService/ICampaignService se les pasa null!: ningún camino de
// CreateAsync/CancelAsync los invoca.
public class ScheduledProspectAutomationServiceTests
{
    private static async Task<(int OrgId, int CampaignId)> SeedOrgWithDraftCampaignAsync(string dbName)
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
            IsActive = true
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();

        var campaign = new Campaign
        {
            OrganizationId = org.Id,
            Name = "Campaña Test",
            Channel = MessagingChannel.Whatsapp,
            MessageTemplateId = template.Id
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        return (org.Id, campaign.Id);
    }

    private static ScheduleProspectAutomationRequest BuildRequest(int campaignId, DateTimeOffset scheduledAt, int radiusKm = 10) =>
        new(["Moreno"], null, radiusKm, 50, campaignId, scheduledAt);

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsAsPendingWithCorrectCampaign()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var scheduledAt = DateTimeOffset.UtcNow.AddHours(2);
        var result = await service.CreateAsync(BuildRequest(campaignId, scheduledAt));

        Assert.True(result.Succeeded);
        Assert.Equal(ScheduledAutomationStatus.Pending, result.Value!.Status);
        Assert.Equal(campaignId, result.Value.CampaignId);
        Assert.Equal("Campaña Test", result.Value.CampaignName);
        Assert.Contains("Moreno", result.Value.Localities);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(1, await assertDb.ScheduledProspectAutomations.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ScheduledAtInThePast_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(campaignId, DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.False(result.Succeeded);
        Assert.Contains("futuro", result.Error);
    }

    [Fact]
    public async Task CreateAsync_RadiusOutOfRange_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(campaignId, DateTimeOffset.UtcNow.AddHours(1), radiusKm: 999));

        Assert.False(result.Succeeded);
        Assert.Contains("radio", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_CampaignNotFound_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(campaignId: 999_999, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("no existe", result.Error);
    }

    [Fact]
    public async Task CreateAsync_CampaignAlreadyRunning_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var campaign = await seedDb.Campaigns.FirstAsync(c => c.Id == campaignId);
            campaign.Status = CampaignStatus.Running;
            await seedDb.SaveChangesAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(campaignId, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("Running", result.Error);
    }

    [Fact]
    public async Task CancelAsync_PendingAutomation_MarksCancelled()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var created = await service.CreateAsync(BuildRequest(campaignId, DateTimeOffset.UtcNow.AddHours(1)));

        var result = await service.CancelAsync(created.Value!.Id);

        Assert.True(result.Succeeded);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var automation = await assertDb.ScheduledProspectAutomations.FirstAsync(a => a.Id == created.Value.Id);
        Assert.Equal(ScheduledAutomationStatus.Cancelled, automation.Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadyRunning_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedOrgWithDraftCampaignAsync(dbName);

        int automationId;
        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var automation = new ScheduledProspectAutomation
            {
                OrganizationId = orgId,
                CreatedByUserId = 1,
                SearchCriteriaJson = """{"Localities":["Moreno"],"Categories":null,"RadiusKm":10,"MaxResults":50}""",
                CampaignId = campaignId,
                ScheduledAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = ScheduledAutomationStatus.Running
            };
            seedDb.ScheduledProspectAutomations.Add(automation);
            await seedDb.SaveChangesAsync();
            automationId = automation.Id;
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CancelAsync(automationId);

        Assert.False(result.Succeeded);
    }
}
