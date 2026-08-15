using Hunter.Application.Campaigning;
using Hunter.Application.Compliance;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Alto #2: MaxMessages/MessagesPerHour/MessagesPerDay se
// guardaban en el modelo de Campaign pero nada los hacía cumplir — una campaña seguía mandando
// mientras hubiera destinatarios Pending sin importar la configuración.
public class CampaignSpendLimitsTests
{
    private static async Task<(int orgId, int campaignId)> SeedRunningCampaignAsync(
        string dbName, int recipientCount, int maxMessages = 1000, int messagesPerHour = 100, int messagesPerDay = 1000)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var template = new MessageTemplate
        {
            OrganizationId = org.Id,
            Name = "Primer contacto",
            Content = "Hola {{business_name}}",
            Channel = MessagingChannel.Whatsapp
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();

        var campaign = new Campaign
        {
            OrganizationId = org.Id,
            Name = "Campaña de prueba",
            Status = CampaignStatus.Running,
            Channel = MessagingChannel.Whatsapp,
            MessageTemplateId = template.Id,
            MessagesPerMinute = recipientCount, // no dejar que el batch de por sí limite el envío
            MaxMessages = maxMessages,
            MessagesPerHour = messagesPerHour,
            MessagesPerDay = messagesPerDay
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        for (var i = 0; i < recipientCount; i++)
        {
            var prospect = new Prospect { OrganizationId = org.Id, BusinessName = $"Prospecto {i}" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();

            db.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = org.Id,
                ProspectId = prospect.Id,
                Channel = ProspectContactChannel.Whatsapp,
                Value = $"549111234{i:D4}",
                IsPrimary = true
            });

            db.CampaignRecipients.Add(new CampaignRecipient
            {
                OrganizationId = org.Id,
                CampaignId = campaign.Id,
                ProspectId = prospect.Id,
                Status = CampaignRecipientStatus.Pending
            });
        }
        await db.SaveChangesAsync();

        return (org.Id, campaign.Id);
    }

    private static CampaignService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int organizationId, int userId = 1) =>
        new(db,
            new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId },
            new SuppressionService(db, new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId }),
            new StubMessageProvider(NullLogger<StubMessageProvider>.Instance));

    [Fact]
    public async Task MaxMessages_Reached_StopsSending_AndCompletesCampaign()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedRunningCampaignAsync(dbName, recipientCount: 2, maxMessages: 1);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var first = await service.ProcessQueueAsync(campaignId);

            // El tope corta el batch antes de mandar de más, no después.
            Assert.True(first.Succeeded);
            Assert.Equal(1, first.Value!.Sent);
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var second = await service.ProcessQueueAsync(campaignId);

            Assert.False(second.Succeeded);
            Assert.Contains("tope de 1 mensajes", second.Error);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(CampaignStatus.Completed, assertDb.Campaigns.Single().Status);
        Assert.Single(assertDb.Messages);
        Assert.Single(assertDb.CampaignRecipients.Where(r => r.Status == CampaignRecipientStatus.Pending));
    }

    [Fact]
    public async Task MessagesPerHour_Reached_PausesCampaign_WithoutTouchingMaxMessages()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedRunningCampaignAsync(dbName, recipientCount: 2, messagesPerHour: 1);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var first = await service.ProcessQueueAsync(campaignId);
            Assert.True(first.Succeeded);
            Assert.Equal(1, first.Value!.Sent);
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var second = await service.ProcessQueueAsync(campaignId);

            Assert.False(second.Succeeded);
            Assert.Contains("tope por hora", second.Error);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(CampaignStatus.Paused, assertDb.Campaigns.Single().Status);
        Assert.Single(assertDb.CampaignRecipients.Where(r => r.Status == CampaignRecipientStatus.Pending));
    }

    [Fact]
    public async Task MessagesPerDay_Reached_PausesCampaign()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedRunningCampaignAsync(dbName, recipientCount: 2, messagesPerHour: 100, messagesPerDay: 1);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var first = await service.ProcessQueueAsync(campaignId);
            Assert.True(first.Succeeded);
            Assert.Equal(1, first.Value!.Sent);
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var second = await service.ProcessQueueAsync(campaignId);

            Assert.False(second.Succeeded);
            Assert.Contains("tope diario", second.Error);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(CampaignStatus.Paused, assertDb.Campaigns.Single().Status);
    }

    [Fact]
    public async Task UnderAllLimits_SendsNormally_CampaignStaysRunning()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId) = await SeedRunningCampaignAsync(dbName, recipientCount: 3);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);
        var result = await service.ProcessQueueAsync(campaignId);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.Value!.Sent);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(CampaignStatus.Running, assertDb.Campaigns.Single().Status);
    }
}
