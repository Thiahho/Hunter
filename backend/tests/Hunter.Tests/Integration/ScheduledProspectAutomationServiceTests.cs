using Hunter.Application.Campaigning;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Cubre solo CreateAsync/CancelAsync (validaciones + persistencia, incluida la resolución
// automática de campaña): RunAsync orquesta IImportService + ICampaignService contra
// OpenStreetMap/WhatsApp reales, que no tiene sentido fakear acá — esos dos servicios
// (ImportService.ImportFromOpenStreetMapAsync/ConfirmAsync, CampaignService.AddRecipientsAsync/
// StartAsync/ProcessQueueAsync) ya están cubiertos por sus propios tests. Por eso a
// IImportService/ICampaignService se les pasa null!: ningún camino de CreateAsync/CancelAsync
// los invoca.
public class ScheduledProspectAutomationServiceTests
{
    // Debe coincidir con ScheduledProspectAutomationService.DefaultCampaignName: no se expone
    // públicamente, así que estos tests fijan el mismo literal para poder sembrar/verificar la
    // campaña "de sistema" que la resolución automática busca o crea.
    private const string DefaultCampaignName = "Prospección automática (WhatsApp)";

    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        return org.Id;
    }

    private static async Task<int> SeedActiveWhatsappTemplateAsync(string dbName, int orgId, string name = "Bienvenida")
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);

        var template = new MessageTemplate
        {
            OrganizationId = orgId,
            Name = name,
            Content = "Hola {{business_name}}!",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();

        return template.Id;
    }

    private static async Task<int> SeedDefaultCampaignAsync(string dbName, int orgId, int templateId, CampaignStatus status = CampaignStatus.Draft)
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);

        var campaign = new Campaign
        {
            OrganizationId = orgId,
            Name = DefaultCampaignName,
            Channel = MessagingChannel.Whatsapp,
            MessageTemplateId = templateId,
            Status = status
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        return campaign.Id;
    }

    private static ScheduleProspectAutomationRequest BuildRequest(DateTimeOffset scheduledAt, int radiusKm = 10) =>
        new(["Moreno"], null, radiusKm, 50, scheduledAt);

    [Fact]
    public async Task CreateAsync_NoExistingDefaultCampaign_AutoCreatesOneWithTheActiveTemplate()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var templateId = await SeedActiveWhatsappTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(2)));

        Assert.True(result.Succeeded);
        Assert.Equal(ScheduledAutomationStatus.Pending, result.Value!.Status);
        Assert.Equal(DefaultCampaignName, result.Value.CampaignName);
        Assert.Contains("Moreno", result.Value.Localities);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var campaign = await assertDb.Campaigns.FirstAsync(c => c.Id == result.Value.CampaignId);
        Assert.Equal(templateId, campaign.MessageTemplateId);
        Assert.Equal(1, await assertDb.Campaigns.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_ExistingEditableDefaultCampaign_ReusesItInsteadOfCreatingAnother()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var templateId = await SeedActiveWhatsappTemplateAsync(dbName, orgId);
        var existingCampaignId = await SeedDefaultCampaignAsync(dbName, orgId, templateId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(2)));

        Assert.True(result.Succeeded);
        Assert.Equal(existingCampaignId, result.Value!.CampaignId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(1, await assertDb.Campaigns.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DefaultCampaignAlreadyRunning_CreatesANewOneInsteadOfReusingIt()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var templateId = await SeedActiveWhatsappTemplateAsync(dbName, orgId);
        var runningCampaignId = await SeedDefaultCampaignAsync(dbName, orgId, templateId, CampaignStatus.Running);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(2)));

        Assert.True(result.Succeeded);
        Assert.NotEqual(runningCampaignId, result.Value!.CampaignId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(2, await assertDb.Campaigns.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_NoActiveWhatsappTemplate_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(2)));

        Assert.False(result.Succeeded);
        Assert.Contains("plantilla", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_MultipleActiveWhatsappTemplates_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedActiveWhatsappTemplateAsync(dbName, orgId, "Bienvenida A");
        await SeedActiveWhatsappTemplateAsync(dbName, orgId, "Bienvenida B");

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(2)));

        Assert.False(result.Succeeded);
        Assert.Contains("más de una", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ScheduledAtInThePast_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedActiveWhatsappTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddMinutes(-5)));

        Assert.False(result.Succeeded);
        Assert.Contains("futuro", result.Error);
    }

    [Fact]
    public async Task CreateAsync_RadiusOutOfRange_ReturnsFailure()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedActiveWhatsappTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var result = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(1), radiusKm: 999));

        Assert.False(result.Succeeded);
        Assert.Contains("radio", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_PendingAutomation_MarksCancelled()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        await SeedActiveWhatsappTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = new ScheduledProspectAutomationService(
            db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 }, null!, null!);

        var created = await service.CreateAsync(BuildRequest(DateTimeOffset.UtcNow.AddHours(1)));

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
        var orgId = await SeedOrgAsync(dbName);
        var templateId = await SeedActiveWhatsappTemplateAsync(dbName, orgId);
        var campaignId = await SeedDefaultCampaignAsync(dbName, orgId, templateId);

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
