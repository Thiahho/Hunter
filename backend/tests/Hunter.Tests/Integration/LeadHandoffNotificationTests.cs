using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// Doc 23, sección 37: "El vendedor recibe el contexto antes de responder". Cubre el
// comportamiento de NotifyAssigneeAsync: cuándo notifica por cada canal (WhatsApp, Telegram),
// cuándo no, y que un fallo de cualquiera de los dos proveedores nunca debe tumbar el
// procesamiento del webhook.
public class LeadHandoffNotificationTests
{
    private static async Task<(int OrgId, int UserId)> SeedOrgWithSellerAsync(
        string dbName, string? sellerPhone, string? sellerTelegramChatId = null)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var seller = new User
        {
            OrganizationId = org.Id, FirstName = "Juan", LastName = "Perez", Email = "juan@difrani.com",
            PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Ventas,
            Phone = sellerPhone, TelegramChatId = sellerTelegramChatId
        };
        db.Users.Add(seller);
        await db.SaveChangesAsync();

        return (org.Id, seller.Id);
    }

    private static async Task<int> SeedProspectAsync(string dbName, int orgId, string phone)
    {
        await using var db = TestDb.Create(dbName);

        var prospect = new Prospect
        {
            OrganizationId = orgId, BusinessName = "Repuestos Oeste", City = "Moreno",
            Category = ProspectCategory.Workshop, CommercialScore = 92
        };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        db.ProspectContacts.Add(new ProspectContact
        {
            OrganizationId = orgId, ProspectId = prospect.Id, Channel = ProspectContactChannel.Whatsapp, Value = phone, IsPrimary = true
        });
        await db.SaveChangesAsync();

        return prospect.Id;
    }

    [Fact]
    public async Task NewLead_AssigneeWithPhone_SendsHandoffSummary()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa, pasame info"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);

        var handoff = Assert.Single(provider.SentRequests);
        Assert.Equal("5491199998888", handoff.ToContact);
        Assert.Contains("Repuestos Oeste", handoff.Content);
        Assert.Contains("Moreno", handoff.Content);
        Assert.Contains("92", handoff.Content);
        Assert.Contains("me interesa, pasame info", handoff.Content);
        Assert.Contains("📱 https://wa.me/5491112345678", handoff.Content); // clic directo al chat, sin ir al CRM
        Assert.Contains("Mi nombre es Juan", handoff.Content); // sugerencia de respuesta con el nombre del vendedor asignado
        Assert.True(handoff.PreferFreeText); // sin HandoffTemplateName configurada, cae a texto libre
    }

    [Fact]
    public async Task NewLead_AssigneeWithoutPhone_DoesNotSendAndStillSucceeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: null);
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);
        Assert.Empty(provider.SentRequests);
    }

    [Fact]
    public async Task NewLead_AssigneeWithTelegramChatIdOnly_SendsTelegramNotWhatsApp()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: null, sellerTelegramChatId: "555111222");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();
        var telegram = new RecordingTelegramNotifier();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, telegram, NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa, pasame info"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);
        Assert.Empty(provider.SentRequests);

        var telegramMessage = Assert.Single(telegram.SentMessages);
        Assert.Equal("555111222", telegramMessage.ChatId);
        Assert.Contains("Repuestos Oeste", telegramMessage.Message);
    }

    [Fact]
    public async Task NewLead_AssigneeWithPhoneAndTelegram_SendsBothChannels()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888", sellerTelegramChatId: "555111222");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();
        var telegram = new RecordingTelegramNotifier();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, telegram, NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.Single(provider.SentRequests);
        Assert.Single(telegram.SentMessages);
    }

    [Fact]
    public async Task NewLead_TelegramFailsButWhatsAppSucceeds_LeadStillPersistsAndWhatsAppWasSent()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888", sellerTelegramChatId: "555111222");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();
        var telegram = new RecordingTelegramNotifier { NextSendSucceeds = false, NextSendError = "bot no autorizado" };

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, telegram, NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);
        Assert.Single(provider.SentRequests); // WhatsApp sí se mandó, pese al fallo de Telegram
        Assert.Single(telegram.SentMessages); // Telegram lo intentó, pero falló

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.True(await assertDb.Leads.AnyAsync(l => l.Id == result.Value.LeadId!.Value));
    }

    [Fact]
    public async Task NewLead_WhatsAppFailsButTelegramSucceeds_LeadStillPersistsAndTelegramWasSent()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888", sellerTelegramChatId: "555111222");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider { NextSendSucceeds = false, NextSendError = "131047: fuera de ventana" };
        var telegram = new RecordingTelegramNotifier();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, telegram, NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);
        Assert.Single(provider.SentRequests);
        Assert.Single(telegram.SentMessages);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.True(await assertDb.Leads.AnyAsync(l => l.Id == result.Value.LeadId!.Value));
    }

    [Fact]
    public async Task SecondInboundMessage_LeadReused_NotifiesOnlyOnce()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider();

        await using (var db1 = TestDb.Create(dbName, organizationId: null))
        {
            var service1 = new InboundMessageService(db1, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);
            var result1 = await service1.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa", ExternalInboundId: "wh-1"));
            Assert.True(result1.Succeeded);
        }

        await using (var db2 = TestDb.Create(dbName, organizationId: null))
        {
            var service2 = new InboundMessageService(db2, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);
            var result2 = await service2.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "dale, me interesa", ExternalInboundId: "wh-2"));
            Assert.True(result2.Succeeded);
        }

        // El segundo mensaje reutiliza el lead abierto (mismo prospecto, lead sigue New):
        // notificar ahí spamearía al vendedor en cada follow-up.
        Assert.Single(provider.SentRequests);
    }

    [Fact]
    public async Task ProviderReturnsFailure_LeadStillPersistsAndProcessingStillSucceeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider { NextSendSucceeds = false, NextSendError = "131047: fuera de ventana" };

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);
        Assert.Single(provider.SentRequests);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.True(await assertDb.Leads.AnyAsync(l => l.Id == result.Value.LeadId!.Value));
    }

    [Fact]
    public async Task ProviderThrows_LeadStillPersistsAndProcessingStillSucceeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellerAsync(dbName, sellerPhone: "5491199998888");
        await SeedProspectAsync(dbName, orgId, "5491112345678");

        var provider = new RecordingMessageProvider { ThrowOnNextSend = true };

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), provider, new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345678", "me interesa"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        Assert.True(await assertDb.Leads.AnyAsync(l => l.Id == result.Value.LeadId!.Value));
    }
}
