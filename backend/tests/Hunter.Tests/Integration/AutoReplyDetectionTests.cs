using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hunter.Tests.Integration;

// Cubre IAutoReplyDetector (frase típica de auto-responder, texto duplicado entre prospectos
// distintos) y el flujo completo en InboundMessageService cuando se detecta una auto-respuesta:
// no genera Lead, no notifica al vendedor, y programa (con tope) un ScheduledMessage de reintento.
public class AutoReplyDetectionTests
{
    // Frase larga sin match de reglas de IAutoReplyDetector ni de KeywordIntentClassifier, para
    // aislar la señal de "texto duplicado entre prospectos distintos" de la señal de frases.
    private const string GenericLongReply = "Buenas tardes, en un rato te contesto con mas detalle sobre esto";

    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    private static async Task<int> SeedProspectAsync(string dbName, int orgId, string phone)
    {
        await using var db = TestDb.Create(dbName);

        var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        db.ProspectContacts.Add(new ProspectContact
        {
            OrganizationId = orgId, ProspectId = prospect.Id, Channel = ProspectContactChannel.Whatsapp, Value = phone, IsPrimary = true
        });
        await db.SaveChangesAsync();

        return prospect.Id;
    }

    private static async Task SeedMessageResponseAsync(string dbName, int orgId, int prospectId, string content)
    {
        await using var db = TestDb.Create(dbName);
        db.MessageResponses.Add(new MessageResponse
        {
            OrganizationId = orgId,
            ProspectId = prospectId,
            Content = content,
            Classification = IntentClassification.Unclear,
            Confidence = 0.4m
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task IsLikelyAutomatedAsync_AutoReplyPhrase_ReturnsTrue()
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName);
        var detector = new AutoReplyDetector(db);

        var result = await detector.IsLikelyAutomatedAsync(
            1, "Gracias por comunicarte con Repuestos Oeste. En este momento no podemos atenderte, en breve nos pondremos en contacto.");

        Assert.True(result);
    }

    [Fact]
    public async Task IsLikelyAutomatedAsync_ShortHumanGreeting_ReturnsFalse()
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName);
        var detector = new AutoReplyDetector(db);

        var result = await detector.IsLikelyAutomatedAsync(1, "hola");

        Assert.False(result);
    }

    [Fact]
    public async Task IsLikelyAutomatedAsync_SameTextFromOnlyOneOtherProspect_ReturnsFalse()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectA = await SeedProspectAsync(dbName, orgId, "5491100000001");
        await SeedMessageResponseAsync(dbName, orgId, prospectA, GenericLongReply);

        await using var db = TestDb.Create(dbName);
        var detector = new AutoReplyDetector(db);

        var result = await detector.IsLikelyAutomatedAsync(orgId, GenericLongReply);

        Assert.False(result);
    }

    [Fact]
    public async Task IsLikelyAutomatedAsync_SameTextFromTwoDistinctProspects_ReturnsTrue()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectA = await SeedProspectAsync(dbName, orgId, "5491100000001");
        var prospectB = await SeedProspectAsync(dbName, orgId, "5491100000002");
        await SeedMessageResponseAsync(dbName, orgId, prospectA, GenericLongReply);
        await SeedMessageResponseAsync(dbName, orgId, prospectB, GenericLongReply);

        await using var db = TestDb.Create(dbName);
        var detector = new AutoReplyDetector(db);

        var result = await detector.IsLikelyAutomatedAsync(orgId, GenericLongReply);

        Assert.True(result);
    }

    private static async Task<int> SeedFollowUpTemplateAsync(string dbName, int orgId)
    {
        await using var db = TestDb.Create(dbName);
        var template = new MessageTemplate
        {
            OrganizationId = orgId,
            Name = "Nudge auto-reply",
            Content = "Hola! ¿Hay alguien que me pueda ayudar por acá?",
            Channel = MessagingChannel.Whatsapp,
            IsActive = true,
            IsFollowUpTemplate = true
        };
        db.MessageTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
    }

    private static InboundMessageService CreateService(Hunter.Infrastructure.Persistence.HunterDbContext db) =>
        new(db, new KeywordIntentClassifier(), new AutoReplyDetector(db), Options.Create(new AutoReplyFollowUpOptions()),
            new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(),
            NullLogger<InboundMessageService>.Instance);

    [Fact]
    public async Task ProcessAsync_AutoReplyPhrase_MarksProspectAndSchedulesFollowUp_WithoutLeadOrNotification()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId, "5491112345678");
        await SeedFollowUpTemplateAsync(dbName, orgId);

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = CreateService(db);

        var result = await service.ProcessAsync(new InboundMessageRequest(
            orgId, "5491112345678", "Gracias por comunicarte con nosotros. En este momento no podemos atenderte."));

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.AutomatedReply, result.Value!.Classification);
        Assert.Null(result.Value.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var prospect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(ProspectStatus.AutoReplyDetected, prospect.Status);
        Assert.Equal(1, prospect.AutoReplyAttempts);
        Assert.False(await assertDb.Leads.AnyAsync(l => l.ProspectId == prospectId));

        var scheduled = await assertDb.ScheduledMessages.SingleAsync(s => s.ProspectId == prospectId);
        Assert.Equal(ScheduledMessageSource.AutoReplyRetry, scheduled.Source);
        Assert.Null(scheduled.CreatedByUserId);
        Assert.Equal(ScheduledMessageStatus.Pending, scheduled.Status);
        Assert.True(scheduled.ScheduledAt > DateTimeOffset.UtcNow.AddHours(2) && scheduled.ScheduledAt < DateTimeOffset.UtcNow.AddHours(4));
    }

    [Fact]
    public async Task ProcessAsync_AutoReplyPhrase_NoFollowUpTemplateConfigured_StillSucceedsWithoutScheduling()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId, "5491112345678");
        // Sin SeedFollowUpTemplateAsync: la organización no tiene plantilla IsFollowUpTemplate.

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = CreateService(db);

        var result = await service.ProcessAsync(new InboundMessageRequest(
            orgId, "5491112345678", "Gracias por comunicarte con nosotros. En este momento no podemos atenderte."));

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.AutomatedReply, result.Value!.Classification);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.False(await assertDb.ScheduledMessages.AnyAsync(s => s.ProspectId == prospectId));
        var prospect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(0, prospect.AutoReplyAttempts);
    }

    [Fact]
    public async Task ProcessAsync_AutoReplyPhrase_AttemptsCapReached_DoesNotScheduleAnotherFollowUp()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var prospectId = await SeedProspectAsync(dbName, orgId, "5491112345678");
        await SeedFollowUpTemplateAsync(dbName, orgId);

        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var seededProspect = await seedDb.Prospects.FirstAsync(p => p.Id == prospectId);
            // Tope por defecto de AutoReplyFollowUpOptions.MaxAutoReplyAttempts es 2.
            seededProspect.AutoReplyAttempts = 2;
            await seedDb.SaveChangesAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = CreateService(db);

        var result = await service.ProcessAsync(new InboundMessageRequest(
            orgId, "5491112345678", "Gracias por comunicarte con nosotros. En este momento no podemos atenderte."));

        Assert.True(result.Succeeded);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.False(await assertDb.ScheduledMessages.AnyAsync(s => s.ProspectId == prospectId));
        var prospect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(2, prospect.AutoReplyAttempts);
    }
}
