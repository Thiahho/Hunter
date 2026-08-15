using Hunter.Application.Campaigning;
using Hunter.Application.Compliance;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// "No enviados" (frontend) muestra tanto CampaignRecipient (envío por campaña) como Message
// sueltos sin CampaignId (envío individual vía TestMessageService). Antes de este fix, un envío
// individual fallido nunca aparecía ahí sin importar el filtro de estado elegido.
public class CampaignRecipientSearchTests
{
    private static CampaignService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int organizationId) =>
        new(db,
            new FakeCurrentUserService { OrganizationId = organizationId, UserId = 1 },
            new SuppressionService(db, new FakeCurrentUserService { OrganizationId = organizationId, UserId = 1 }),
            new StubMessageProvider(NullLogger<StubMessageProvider>.Instance));

    private static async Task<int> SeedOrgAsync(Hunter.Infrastructure.Persistence.HunterDbContext db)
    {
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    [Fact]
    public async Task SearchRecipients_StatusFailed_IncludesAdHocFailedMessage()
    {
        var dbName = TestDb.NewDbName();
        int orgId, prospectId;

        await using (var db = TestDb.Create(dbName))
        {
            orgId = await SeedOrgAsync(db);
            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Envío Individual" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();
            prospectId = prospect.Id;

            db.Messages.Add(new Message
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                CampaignId = null,
                CampaignRecipientId = null,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow,
                FailureReason = "Número inválido"
            });
            await db.SaveChangesAsync();
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(assertDb, orgId);
        var result = await service.SearchRecipientsAsync(search: null, campaignId: null, status: CampaignRecipientStatus.Failed, page: 1, pageSize: 30);

        var item = Assert.Single(result.Items);
        Assert.False(item.IsCampaignRecipient);
        Assert.Null(item.CampaignId);
        Assert.Equal(prospectId, item.ProspectId);
        Assert.Equal(CampaignRecipientStatus.Failed, item.Status);
        Assert.Equal("Número inválido", item.LastMessageFailureReason);
    }

    [Fact]
    public async Task SearchRecipients_StatusFailed_CombinesCampaignRecipientAndAdHocMessage()
    {
        var dbName = TestDb.NewDbName();
        int orgId;

        await using (var db = TestDb.Create(dbName))
        {
            orgId = await SeedOrgAsync(db);

            var template = new MessageTemplate
            {
                OrganizationId = orgId,
                Name = "Plantilla",
                Content = "Hola {{business_name}}",
                Channel = MessagingChannel.Whatsapp
            };
            db.MessageTemplates.Add(template);

            var prospectA = new Prospect { OrganizationId = orgId, BusinessName = "Por campaña" };
            var prospectB = new Prospect { OrganizationId = orgId, BusinessName = "Individual" };
            db.Prospects.AddRange(prospectA, prospectB);
            await db.SaveChangesAsync();

            var campaign = new Campaign
            {
                OrganizationId = orgId,
                Name = "Campaña X",
                Status = CampaignStatus.Running,
                Channel = MessagingChannel.Whatsapp,
                MessageTemplateId = template.Id
            };
            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();

            db.CampaignRecipients.Add(new CampaignRecipient
            {
                OrganizationId = orgId,
                CampaignId = campaign.Id,
                ProspectId = prospectA.Id,
                Status = CampaignRecipientStatus.Failed
            });

            db.Messages.Add(new Message
            {
                OrganizationId = orgId,
                ProspectId = prospectB.Id,
                CampaignId = null,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync();
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(assertDb, orgId);
        var result = await service.SearchRecipientsAsync(search: null, campaignId: null, status: CampaignRecipientStatus.Failed, page: 1, pageSize: 30);

        Assert.Equal(2, result.TotalItems);
        Assert.Contains(result.Items, i => i.IsCampaignRecipient);
        Assert.Contains(result.Items, i => !i.IsCampaignRecipient);
    }

    [Fact]
    public async Task SearchRecipients_FilteredByCampaignId_ExcludesAdHocMessages()
    {
        var dbName = TestDb.NewDbName();
        int orgId, campaignId;

        await using (var db = TestDb.Create(dbName))
        {
            orgId = await SeedOrgAsync(db);

            var template = new MessageTemplate
            {
                OrganizationId = orgId,
                Name = "Plantilla",
                Content = "Hola",
                Channel = MessagingChannel.Whatsapp
            };
            db.MessageTemplates.Add(template);

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Individual" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();

            var campaign = new Campaign
            {
                OrganizationId = orgId,
                Name = "Campaña X",
                Status = CampaignStatus.Running,
                Channel = MessagingChannel.Whatsapp,
                MessageTemplateId = template.Id
            };
            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();
            campaignId = campaign.Id;

            db.Messages.Add(new Message
            {
                OrganizationId = orgId,
                ProspectId = prospect.Id,
                CampaignId = null,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(assertDb, orgId);
        var result = await service.SearchRecipientsAsync(search: null, campaignId: campaignId, status: CampaignRecipientStatus.Failed, page: 1, pageSize: 30);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task DeleteRecipient_RemovesRow_WithoutTouchingItsMessages()
    {
        var dbName = TestDb.NewDbName();
        int orgId, recipientId, messageId;

        await using (var db = TestDb.Create(dbName))
        {
            orgId = await SeedOrgAsync(db);

            var template = new MessageTemplate
            {
                OrganizationId = orgId,
                Name = "Plantilla",
                Content = "Hola",
                Channel = MessagingChannel.Whatsapp
            };
            db.MessageTemplates.Add(template);

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Por campaña" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();

            var campaign = new Campaign
            {
                OrganizationId = orgId,
                Name = "Campaña X",
                Status = CampaignStatus.Running,
                Channel = MessagingChannel.Whatsapp,
                MessageTemplateId = template.Id
            };
            db.Campaigns.Add(campaign);
            await db.SaveChangesAsync();

            var message = new Message
            {
                OrganizationId = orgId,
                ProspectId = prospect.Id,
                CampaignId = campaign.Id,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;

            var recipient = new CampaignRecipient
            {
                OrganizationId = orgId,
                CampaignId = campaign.Id,
                ProspectId = prospect.Id,
                Status = CampaignRecipientStatus.Failed,
                LastMessageId = message.Id
            };
            db.CampaignRecipients.Add(recipient);
            await db.SaveChangesAsync();
            recipientId = recipient.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.DeleteRecipientAsync(recipientId);
            Assert.True(result.Succeeded);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Empty(assertDb.CampaignRecipients);
        Assert.Single(assertDb.Messages.Where(m => m.Id == messageId));
    }

    [Fact]
    public async Task DeleteRecipients_Bulk_RemovesOnlyRequestedRows()
    {
        var dbName = TestDb.NewDbName();
        int orgId, keepId, deleteAId, deleteBId;

        await using (var db = TestDb.Create(dbName))
        {
            orgId = await SeedOrgAsync(db);

            var template = new MessageTemplate
            {
                OrganizationId = orgId,
                Name = "Plantilla",
                Content = "Hola",
                Channel = MessagingChannel.Whatsapp
            };
            db.MessageTemplates.Add(template);

            var campaign = new Campaign
            {
                OrganizationId = orgId,
                Name = "Campaña X",
                Status = CampaignStatus.Running,
                Channel = MessagingChannel.Whatsapp,
                MessageTemplateId = template.Id
            };
            db.Campaigns.Add(campaign);

            var prospects = new[]
            {
                new Prospect { OrganizationId = orgId, BusinessName = "A" },
                new Prospect { OrganizationId = orgId, BusinessName = "B" },
                new Prospect { OrganizationId = orgId, BusinessName = "C" }
            };
            db.Prospects.AddRange(prospects);
            await db.SaveChangesAsync();

            var recipients = prospects.Select(p => new CampaignRecipient
            {
                OrganizationId = orgId,
                CampaignId = campaign.Id,
                ProspectId = p.Id,
                Status = CampaignRecipientStatus.Failed
            }).ToArray();
            db.CampaignRecipients.AddRange(recipients);
            await db.SaveChangesAsync();

            keepId = recipients[0].Id;
            deleteAId = recipients[1].Id;
            deleteBId = recipients[2].Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.DeleteRecipientsAsync([deleteAId, deleteBId]);
            Assert.True(result.Succeeded);
            Assert.Equal(2, result.Value);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var remaining = Assert.Single(assertDb.CampaignRecipients);
        Assert.Equal(keepId, remaining.Id);
    }
}
