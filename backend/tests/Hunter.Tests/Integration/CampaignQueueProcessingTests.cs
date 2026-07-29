using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Compliance;
using Hunter.Application.Compliance.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// Casos críticos obligatorios de doc 10 (Sprint 9) y doc 13 (Seguridad y Cumplimiento):
// Kill Switch corta el envío, contactos suprimidos nunca reciben mensajes, y reprocesar
// la cola no reenvía a un destinatario que ya fue enviado ("mismo ExternalMessageId no duplica").
public class CampaignQueueProcessingTests
{
    private static async Task<(int orgId, int campaignId, int prospectId)> SeedRunningCampaignAsync(
        string dbName, string contactValue = "5491112345678")
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

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        db.ProspectContacts.Add(new ProspectContact
        {
            OrganizationId = org.Id,
            ProspectId = prospect.Id,
            Channel = ProspectContactChannel.Whatsapp,
            Value = contactValue,
            IsPrimary = true
        });

        var campaign = new Campaign
        {
            OrganizationId = org.Id,
            Name = "Campaña de prueba",
            Status = CampaignStatus.Running,
            Channel = MessagingChannel.Whatsapp,
            MessageTemplateId = template.Id
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        db.CampaignRecipients.Add(new CampaignRecipient
        {
            OrganizationId = org.Id,
            CampaignId = campaign.Id,
            ProspectId = prospect.Id,
            Status = CampaignRecipientStatus.Pending
        });
        await db.SaveChangesAsync();

        return (org.Id, campaign.Id, prospect.Id);
    }

    private static CampaignService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int organizationId, int userId = 1) =>
        new(db,
            new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId },
            new SuppressionService(db, new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId }),
            new StubMessageProvider(NullLogger<StubMessageProvider>.Instance));

    [Fact]
    public async Task KillSwitchEnabled_Blocks_ProcessQueue()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId, _) = await SeedRunningCampaignAsync(dbName);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.SetKillSwitchAsync(new KillSwitchRequest(true, "prueba"));
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.ProcessQueueAsync(campaignId);

            Assert.False(result.Succeeded);
            Assert.Contains("Kill Switch", result.Error);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var recipient = assertDb.CampaignRecipients.Single();
        Assert.Equal(CampaignRecipientStatus.Pending, recipient.Status);
        Assert.Empty(assertDb.Messages);
    }

    [Fact]
    public async Task SuppressedContact_Is_Skipped_And_Never_Sent()
    {
        var dbName = TestDb.NewDbName();
        const string contact = "5491112345678";
        var (orgId, campaignId, _) = await SeedRunningCampaignAsync(dbName, contact);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var suppressionService = new SuppressionService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });
            var result = await suppressionService.CreateAsync(
                new CreateSuppressionRequest(contact, SuppressionContactType.Whatsapp, SuppressionReason.UserRequested, "test"));
            Assert.True(result.Succeeded);
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.ProcessQueueAsync(campaignId);

            Assert.True(result.Succeeded);
            Assert.Equal(0, result.Value!.Sent);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var recipient = assertDb.CampaignRecipients.Single();
        Assert.Equal(CampaignRecipientStatus.Stopped, recipient.Status);
        Assert.Empty(assertDb.Messages);
    }

    [Fact]
    public async Task ProcessQueue_Called_Twice_Does_Not_Resend_To_Already_Sent_Recipient()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, campaignId, _) = await SeedRunningCampaignAsync(dbName);

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

            // El único destinatario ya quedó en estado Sent tras la primera corrida:
            // la segunda no vuelve a tomarlo (el filtro solo levanta Pending/Queued).
            Assert.True(second.Succeeded);
            Assert.Equal(0, second.Value!.Processed);
            Assert.Equal(0, second.Value.Sent);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Single(assertDb.Messages);
        Assert.Single(assertDb.CampaignRecipients.Where(r => r.Status == CampaignRecipientStatus.Sent));
    }
}
