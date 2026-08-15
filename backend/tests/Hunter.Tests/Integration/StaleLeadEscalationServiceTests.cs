using Hunter.Application.Crm;
using Hunter.Domain.Crm;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Medio "StaleLeadEscalationService no tiene ningún test":
// nada verificaba el umbral por organización, el anti-spam de LastEscalatedAt, ni el fallback
// cuando el vendedor asignado no tiene TelegramChatId cargado.
public class StaleLeadEscalationServiceTests
{
    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static async Task<int> SeedAssigneeAsync(string dbName, int orgId, string? telegramChatId = "123456")
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var user = new User
        {
            OrganizationId = orgId,
            FirstName = "Vendedor",
            LastName = "Uno",
            Email = $"vendedor{Guid.NewGuid():N}@difrani.com",
            PasswordHash = "irrelevant",
            IsActive = true,
            TelegramChatId = telegramChatId
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private static async Task<int> SeedStaleLeadAsync(
        string dbName, int orgId, int assigneeId, DateTimeOffset lastActivityAt,
        DateTimeOffset? lastEscalatedAt = null, LeadStatus status = LeadStatus.New)
    {
        await using var db = TestDb.Create(dbName, organizationId: orgId);

        var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        db.ProspectContacts.Add(new ProspectContact
        {
            OrganizationId = orgId,
            ProspectId = prospect.Id,
            Channel = ProspectContactChannel.Whatsapp,
            Value = "5491112345678",
            IsPrimary = true,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var lead = new Lead
        {
            OrganizationId = orgId,
            ProspectId = prospect.Id,
            AssignedToUserId = assigneeId,
            Status = status,
            LastActivityAt = lastActivityAt,
            LastEscalatedAt = lastEscalatedAt
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return lead.Id;
    }

    private static StaleLeadEscalationService CreateService(
        Hunter.Infrastructure.Persistence.HunterDbContext db, RecordingTelegramNotifier notifier) =>
        new(db, notifier, NullLogger<StaleLeadEscalationService>.Instance);

    [Fact]
    public async Task EscalateStaleLeadsAsync_LeadPastThreshold_NotifiesAndStampsLastEscalatedAt()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        var leadId = await SeedStaleLeadAsync(dbName, orgId, assigneeId, DateTimeOffset.UtcNow.AddHours(-25));

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(1, escalated);
        Assert.Single(notifier.SentMessages);
        Assert.Equal("123456", notifier.SentMessages[0].ChatId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var lead = await assertDb.Leads.FirstAsync(l => l.Id == leadId);
        Assert.NotNull(lead.LastEscalatedAt);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_LeadUnderThreshold_DoesNotNotify()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        await SeedStaleLeadAsync(dbName, orgId, assigneeId, DateTimeOffset.UtcNow.AddHours(-2));

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(0, escalated);
        Assert.Empty(notifier.SentMessages);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_AlreadyEscalatedRecently_DoesNotSpam()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        // Sin actividad hace 48hs, pero ya se avisó hace 2hs: dentro del mismo umbral de 24hs
        // desde el último aviso, no debe repetirlo en cada tick.
        await SeedStaleLeadAsync(
            dbName, orgId, assigneeId,
            lastActivityAt: DateTimeOffset.UtcNow.AddHours(-48),
            lastEscalatedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(0, escalated);
        Assert.Empty(notifier.SentMessages);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_EscalatedLongAgo_RemindsAgain()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        // Último aviso hace 30hs (más que el umbral de 24hs desde el último aviso): corresponde
        // volver a avisar.
        await SeedStaleLeadAsync(
            dbName, orgId, assigneeId,
            lastActivityAt: DateTimeOffset.UtcNow.AddHours(-72),
            lastEscalatedAt: DateTimeOffset.UtcNow.AddHours(-30));

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(1, escalated);
        Assert.Single(notifier.SentMessages);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_AssigneeWithoutTelegramChatId_SkipsNotificationButDoesNotThrow()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId, telegramChatId: null);
        await SeedStaleLeadAsync(dbName, orgId, assigneeId, DateTimeOffset.UtcNow.AddHours(-25));

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(0, escalated);
        Assert.Empty(notifier.SentMessages);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_CustomOrganizationThreshold_OverridesDefault()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        // 3hs sin actividad: por debajo del default (24hs), pero por encima de un umbral
        // configurado en 1hs.
        await SeedStaleLeadAsync(dbName, orgId, assigneeId, DateTimeOffset.UtcNow.AddHours(-3));

        await using (var db = TestDb.Create(dbName, organizationId: orgId))
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = orgId,
                Key = OrganizationSettingsKeys.StaleLeadEscalationHours,
                Value = "1"
            });
            await db.SaveChangesAsync();
        }

        var notifier = new RecordingTelegramNotifier();
        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(assertDb, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(1, escalated);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_WonLead_IsNeverEscalated()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var assigneeId = await SeedAssigneeAsync(dbName, orgId);
        await SeedStaleLeadAsync(dbName, orgId, assigneeId, DateTimeOffset.UtcNow.AddHours(-100), status: LeadStatus.Won);

        var notifier = new RecordingTelegramNotifier();
        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(db, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(0, escalated);
        Assert.Empty(notifier.SentMessages);
    }

    [Fact]
    public async Task EscalateStaleLeadsAsync_UnassignedLead_IsNeverEscalated()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using (var db = TestDb.Create(dbName, organizationId: orgId))
        {
            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
            db.Prospects.Add(prospect);
            await db.SaveChangesAsync();

            db.Leads.Add(new Lead
            {
                OrganizationId = orgId,
                ProspectId = prospect.Id,
                AssignedToUserId = null,
                Status = LeadStatus.New,
                LastActivityAt = DateTimeOffset.UtcNow.AddHours(-100)
            });
            await db.SaveChangesAsync();
        }

        var notifier = new RecordingTelegramNotifier();
        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var service = CreateService(assertDb, notifier);

        var escalated = await service.EscalateStaleLeadsAsync();

        Assert.Equal(0, escalated);
        Assert.Empty(notifier.SentMessages);
    }
}
