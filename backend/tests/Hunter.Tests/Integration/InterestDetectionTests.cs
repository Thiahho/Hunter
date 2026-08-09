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

// Casos críticos obligatorios de doc 07 (Epic08/Epic14) / doc 10 (Sprint 9):
// "Interés detectado GENERA Lead" y la regla de idempotencia de eventos de webhook.
public class InterestDetectionTests
{
    [Fact]
    public async Task Interested_Response_Above_Threshold_Creates_Lead_And_Assigns_Seller()
    {
        var dbName = TestDb.NewDbName();
        int orgId, prospectId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();
            orgId = org.Id;

            var seller = new Hunter.Domain.Identity.User
            {
                OrganizationId = orgId,
                FirstName = "Juan",
                LastName = "Perez",
                Email = "juan@difrani.com",
                PasswordHash = "irrelevant"
            };
            seedDb.Users.Add(seller);

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();
            prospectId = prospect.Id;

            seedDb.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                Channel = ProspectContactChannel.Whatsapp,
                Value = "5491112345678",
                IsPrimary = true
            });
            await seedDb.SaveChangesAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new AutoReplyDetector(db), Options.Create(new AutoReplyFollowUpOptions()), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var request = new InboundMessageRequest(orgId, "5491112345678", "me interesa, pasame informacion", ExternalInboundId: "wh-1");
        var result = await service.ProcessAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.Interested, result.Value!.Classification);
        Assert.NotNull(result.Value.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var lead = await assertDb.Leads.FirstAsync(l => l.Id == result.Value.LeadId!.Value);
        Assert.Equal(prospectId, lead.ProspectId);
        Assert.NotNull(lead.AssignedToUserId);

        var updatedProspect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(ProspectStatus.Lead, updatedProspect.Status);
    }

    [Fact]
    public async Task Same_ExternalInboundId_Processed_Twice_Does_Not_Duplicate_Lead()
    {
        var dbName = TestDb.NewDbName();
        int orgId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();
            orgId = org.Id;

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();

            seedDb.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = orgId,
                ProspectId = prospect.Id,
                Channel = ProspectContactChannel.Whatsapp,
                Value = "5491112345678",
                IsPrimary = true
            });
            await seedDb.SaveChangesAsync();
        }

        var request = new InboundMessageRequest(orgId, "5491112345678", "me interesa", ExternalInboundId: "wh-repeated");

        int? firstLeadId;
        await using (var db1 = TestDb.Create(dbName, organizationId: null))
        {
            var result1 = await new InboundMessageService(db1, new KeywordIntentClassifier(), new AutoReplyDetector(db1), Options.Create(new AutoReplyFollowUpOptions()), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance).ProcessAsync(request);
            Assert.True(result1.Succeeded);
            firstLeadId = result1.Value!.LeadId;
        }

        await using (var db2 = TestDb.Create(dbName, organizationId: null))
        {
            var result2 = await new InboundMessageService(db2, new KeywordIntentClassifier(), new AutoReplyDetector(db2), Options.Create(new AutoReplyFollowUpOptions()), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance).ProcessAsync(request);
            Assert.True(result2.Succeeded);
            Assert.Equal(firstLeadId, result2.Value!.LeadId);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var leadCount = await assertDb.Leads.CountAsync();
        var responseCount = await assertDb.MessageResponses.IgnoreQueryFilters().CountAsync();

        Assert.Equal(1, leadCount);
        Assert.Equal(1, responseCount);
    }

    // Regresión del bug real: un contacto cargado a mano sin el "9" móvil (ej. "541122602000")
    // no matcheaba contra un inbound real de Meta, que siempre lo trae en "from"
    // (ej. "5491122602000") — el prospecto "desaparecía" para el webhook aunque existiera.
    [Fact]
    public async Task Inbound_From_Number_With_Mobile9_Matches_Contact_Stored_Without_It()
    {
        var dbName = TestDb.NewDbName();
        int orgId, prospectId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();
            orgId = org.Id;

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Test" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();
            prospectId = prospect.Id;

            seedDb.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                Channel = ProspectContactChannel.Whatsapp,
                Value = "541122602000", // cargado a mano, sin el "9"
                IsPrimary = true
            });
            await seedDb.SaveChangesAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new AutoReplyDetector(db), Options.Create(new AutoReplyFollowUpOptions()), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var request = new InboundMessageRequest(orgId, "5491122602000", "hola me interesa", ExternalInboundId: "wh-9-fallback");
        var result = await service.ProcessAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.Interested, result.Value!.Classification);
        Assert.NotNull(result.Value.LeadId);
    }

    [Fact]
    public async Task Stop_Response_Creates_Suppression_And_Blocks_Prospect()
    {
        var dbName = TestDb.NewDbName();
        int orgId, prospectId;

        await using (var seedDb = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            seedDb.Organizations.Add(org);
            await seedDb.SaveChangesAsync();
            orgId = org.Id;

            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Taller Juan" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();
            prospectId = prospect.Id;

            seedDb.ProspectContacts.Add(new ProspectContact
            {
                OrganizationId = orgId,
                ProspectId = prospectId,
                Channel = ProspectContactChannel.Whatsapp,
                Value = "5491198765432",
                IsPrimary = true
            });
            await seedDb.SaveChangesAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: null);
        var result = await new InboundMessageService(db, new KeywordIntentClassifier(), new AutoReplyDetector(db), Options.Create(new AutoReplyFollowUpOptions()), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance)
            .ProcessAsync(new InboundMessageRequest(orgId, "5491198765432", "no me contacten mas, borrame"));

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.Stop, result.Value!.Classification);
        Assert.Null(result.Value.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var updatedProspect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(ProspectStatus.Suppressed, updatedProspect.Status);

        var suppressed = await assertDb.Suppressions.AnyAsync(s => s.Contact == "5491198765432");
        Assert.True(suppressed);
    }
}
