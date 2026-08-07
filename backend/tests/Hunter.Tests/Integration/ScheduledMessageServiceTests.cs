using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Compliance;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// "Programar mensaje" en la ficha del prospecto: un ScheduledMessage con MessageTemplate +
// ScheduledAt, sin recurrencia (V1), que ScheduledMessageBackgroundService dispara llamando a
// RunAsync cuando vence — ver ese archivo y ScheduledMessageService.
public class ScheduledMessageServiceTests
{
    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static async Task<int> SeedProspectAsync(string dbName, int orgId, string? whatsapp = "5491112345678")
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);

        var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        if (whatsapp is not null)
        {
            db.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = orgId,
                ProspectId = prospect.Id,
                Channel = ProspectContactChannel.Whatsapp,
                Value = whatsapp,
                IsPrimary = true
            });
            await db.SaveChangesAsync();
        }

        return prospect.Id;
    }

    private static async Task<int> SeedTemplateAsync(
        string dbName, int orgId, MessagingChannel channel = MessagingChannel.Whatsapp, bool isActive = true, string content = "Hola {{business_name}}!")
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);

        var template = new MessageTemplate
        {
            OrganizationId = orgId,
            Name = "Bienvenida",
            Content = content,
            Channel = channel,
            IsActive = isActive
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();

        return template.Id;
    }

    private static ScheduledMessageService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, int organizationId, int userId = 1) =>
        new(db,
            new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId },
            new TestMessageService(
                db,
                new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId },
                new SuppressionService(db, new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId }),
                new StubMessageProvider(NullLogger<StubMessageProvider>.Instance)));

    [Fact]
    public async Task CreateAsync_ScheduledAtInThePast_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddMinutes(-1)));

        Assert.False(result.Succeeded);
        Assert.Contains("futuro", result.Error);
    }

    [Fact]
    public async Task CreateAsync_ProspectNotFound_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var templateId = await SeedTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(999, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("Prospecto", result.Error);
    }

    [Fact]
    public async Task CreateAsync_InactiveTemplate_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId, isActive: false);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("activa", result.Error);
    }

    [Fact]
    public async Task CreateAsync_NonWhatsappTemplate_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId, channel: MessagingChannel.Email);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddHours(1)));

        Assert.False(result.Succeeded);
        Assert.Contains("WhatsApp", result.Error);
    }

    [Fact]
    public async Task CreateAsync_Valid_PersistsPendingScheduledMessage()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId);
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(2);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, scheduledAt));

        Assert.True(result.Succeeded);
        Assert.Equal(ScheduledMessageStatus.Pending, result.Value!.Status);
        Assert.Equal(prospectId, result.Value.ProspectId);
        Assert.Equal(templateId, result.Value.MessageTemplateId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Single(assertDb.ScheduledMessages);
    }

    [Fact]
    public async Task CancelAsync_PendingScheduledMessage_Cancels()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId);
        int scheduledId;

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var created = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddHours(1)));
            scheduledId = created.Value!.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.CancelAsync(scheduledId);
            Assert.True(result.Succeeded);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Equal(ScheduledMessageStatus.Cancelled, assertDb.ScheduledMessages.Single().Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadySent_Fails()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId);
        int scheduledId;

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var created = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddHours(1)));
            scheduledId = created.Value!.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.RunAsync(scheduledId);
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var result = await service.CancelAsync(scheduledId);
            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public async Task RunAsync_ProspectWithWhatsapp_SendsAndMarksSent()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId, content: "Hola {{business_name}}, tenemos catálogo nuevo.");
        int scheduledId;

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var created = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddMinutes(1)));
            scheduledId = created.Value!.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.RunAsync(scheduledId);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var scheduled = assertDb.ScheduledMessages.Single();
        Assert.Equal(ScheduledMessageStatus.Sent, scheduled.Status);
        Assert.NotNull(scheduled.RunAt);
        Assert.NotNull(scheduled.MessageId);

        var message = Assert.Single(assertDb.Messages);
        Assert.Equal("Hola Repuestos Oeste, tenemos catálogo nuevo.", message.Content);
    }

    [Fact]
    public async Task RunAsync_ProspectWithoutWhatsappContact_MarksFailed()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId, whatsapp: null);
        var templateId = await SeedTemplateAsync(dbName, orgId);
        int scheduledId;

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var created = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddMinutes(1)));
            scheduledId = created.Value!.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.RunAsync(scheduledId);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var scheduled = assertDb.ScheduledMessages.Single();
        Assert.Equal(ScheduledMessageStatus.Failed, scheduled.Status);
        Assert.Contains("WhatsApp", scheduled.FailureReason);
    }

    [Fact]
    public async Task RunAsync_AlreadyRun_IsNoop()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId);
        var templateId = await SeedTemplateAsync(dbName, orgId);
        int scheduledId;

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            var created = await service.CreateAsync(prospectId, new ScheduleMessageRequest(templateId, DateTimeOffset.UtcNow.AddMinutes(1)));
            scheduledId = created.Value!.Id;
        }

        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.RunAsync(scheduledId);
        }

        // Segunda corrida (ej. un tick de más del background service): no debe reenviar.
        await using (var db = TestDb.Create(dbName, organizationId: orgId, userId: 1))
        {
            var service = CreateService(db, orgId);
            await service.RunAsync(scheduledId);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.Single(assertDb.Messages);
    }
}
