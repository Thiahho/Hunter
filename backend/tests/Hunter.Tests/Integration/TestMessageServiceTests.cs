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

// Envío manual de prueba a un prospecto puntual (ej. tu número personal durante desarrollo),
// sin pasar por el flujo de Campaign. Debe respetar las mismas reglas de cumplimiento que
// el envío masivo: Kill Switch y lista de exclusión.
public class TestMessageServiceTests
{
    private static async Task<(int orgId, int prospectId)> SeedProspectAsync(string dbName, string? whatsapp = "5491112345678")
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Prueba Personal" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        if (whatsapp is not null)
        {
            db.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = org.Id,
                ProspectId = prospect.Id,
                Channel = ProspectContactChannel.Whatsapp,
                Value = whatsapp,
                IsPrimary = true
            });
            await db.SaveChangesAsync();
        }

        return (org.Id, prospect.Id);
    }

    private static TestMessageService CreateService(Hunter.Infrastructure.Persistence.HunterDbContext db, int organizationId) =>
        new(db,
            new FakeCurrentUserService { OrganizationId = organizationId, UserId = 1 },
            new SuppressionService(db, new FakeCurrentUserService { OrganizationId = organizationId, UserId = 1 }),
            new StubMessageProvider(NullLogger<StubMessageProvider>.Instance));

    [Fact]
    public async Task SendAsync_ProspectWithWhatsapp_SendsAndRecordsMessage()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.SendAsync(prospectId, new SendTestMessageRequest("Hola {{business_name}}, te contactamos de Hunter."));

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Success);
        Assert.NotNull(result.Value.ExternalMessageId);

        var message = Assert.Single(db.Messages);
        Assert.Equal("Hola Prueba Personal, te contactamos de Hunter.", message.Content);
        Assert.Null(message.CampaignId);
        Assert.Equal(MessageStatus.Sent, message.Status);
    }

    [Fact]
    public async Task SendAsync_ProspectWithoutWhatsappContact_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName, whatsapp: null);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.SendAsync(prospectId, new SendTestMessageRequest("Hola"));

        Assert.False(result.Succeeded);
        Assert.Contains("WhatsApp", result.Error);
        Assert.Empty(db.Messages);
    }

    [Fact]
    public async Task SendAsync_KillSwitchEnabled_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = orgId,
                Key = OrganizationSettingsKeys.KillSwitch,
                Value = "true"
            });
            await db.SaveChangesAsync();
        }

        await using var db2 = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db2, orgId);
        var result = await service.SendAsync(prospectId, new SendTestMessageRequest("Hola"));

        Assert.False(result.Succeeded);
        Assert.Contains("Kill Switch", result.Error);
        Assert.Empty(db2.Messages);
    }

    [Fact]
    public async Task SendAsync_SuppressedContact_Fails()
    {
        var dbName = TestDb.NewDbName();
        const string contact = "5491112345678";
        var (orgId, prospectId) = await SeedProspectAsync(dbName, contact);

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var suppressionService = new SuppressionService(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = 1 });
            await suppressionService.CreateAsync(
                new CreateSuppressionRequest(contact, SuppressionContactType.Whatsapp, SuppressionReason.UserRequested, "test"));
        }

        await using var db2 = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db2, orgId);
        var result = await service.SendAsync(prospectId, new SendTestMessageRequest("Hola"));

        Assert.False(result.Succeeded);
        Assert.Contains("exclusión", result.Error);
        Assert.Empty(db2.Messages);
    }

    [Fact]
    public async Task RetryAsync_FailedAdHocMessage_SendsAgainWithSameContent()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName);
        int messageId;

        await using (var db = TestDb.Create(dbName))
        {
            var message = new Message
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                CampaignId = null,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola Prueba Personal",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await using var db2 = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db2, orgId);
        var result = await service.RetryAsync(messageId);

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.Success);
        Assert.Equal(2, db2.Messages.Count());
        Assert.Contains(db2.Messages, m => m.Status == MessageStatus.Sent && m.Content == "Hola Prueba Personal");
    }

    [Fact]
    public async Task RetryAsync_MessageBelongsToCampaign_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName);
        int messageId;

        await using (var db = TestDb.Create(dbName))
        {
            var message = new Message
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                CampaignId = 1,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Failed,
                FailedAt = DateTimeOffset.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await using var db2 = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db2, orgId);
        var result = await service.RetryAsync(messageId);

        Assert.False(result.Succeeded);
        Assert.Contains("campaña", result.Error);
    }

    [Fact]
    public async Task RetryAsync_MessageNotFailed_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, prospectId) = await SeedProspectAsync(dbName);
        int messageId;

        await using (var db = TestDb.Create(dbName))
        {
            var message = new Message
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                CampaignId = null,
                Channel = MessagingChannel.Whatsapp,
                Provider = "stub",
                Content = "Hola",
                Status = MessageStatus.Sent,
                SentAt = DateTimeOffset.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await using var db2 = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db2, orgId);
        var result = await service.RetryAsync(messageId);

        Assert.False(result.Succeeded);
        Assert.Contains("Falló", result.Error);
    }
}
