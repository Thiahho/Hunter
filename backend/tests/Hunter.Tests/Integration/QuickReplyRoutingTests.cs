using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Messaging;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hunter.Tests.Integration;

// Caso de negocio: el botón quick-reply de WhatsApp es un único CTA genérico ("Estoy
// interesado"), no una autodeclaración de rubro. El tap fuerza Interested con confianza
// máxima, pero el ruteo Administración/Ventas sigue leyendo Prospect.Category tal cual esté
// cargado de antes (import, carga manual, etc.) — el botón no lo toca.
public class QuickReplyRoutingTests
{
    private static async Task<(int OrgId, int AdminUserId, int VentasUserId)> SeedOrgWithBothAreasAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var adminUser = new User
        {
            OrganizationId = org.Id, FirstName = "Ana", LastName = "Admin", Email = "ana@difrani.com",
            PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Administracion
        };
        var ventasUser = new User
        {
            OrganizationId = org.Id, FirstName = "Vero", LastName = "Ventas", Email = "vero@difrani.com",
            PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Ventas
        };
        db.Users.AddRange(adminUser, ventasUser);
        await db.SaveChangesAsync();

        return (org.Id, adminUser.Id, ventasUser.Id);
    }

    private static async Task<int> SeedProspectWithWhatsappAsync(string dbName, int orgId, string phone, ProspectCategory category = ProspectCategory.Workshop)
    {
        await using var db = TestDb.Create(dbName);

        var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Oeste", Category = category };
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
    public async Task InterestButtonTap_ForDistributorProspect_RoutesToAdministracion_CategoryUnchanged()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, adminUserId, _) = await SeedOrgWithBothAreasAsync(dbName);
        var prospectId = await SeedProspectWithWhatsappAsync(dbName, orgId, "5491112345678", ProspectCategory.Distributor);

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var request = new InboundMessageRequest(
            orgId, "5491112345678", "Estoy interesado", ExternalInboundId: "wh-btn-1", ButtonPayload: QuickReplyPayloads.Interested);
        var result = await service.ProcessAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.Interested, result.Value!.Classification);
        Assert.Equal(1.00m, result.Value.Confidence);
        Assert.NotNull(result.Value.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var prospect = await assertDb.Prospects.FirstAsync(p => p.Id == prospectId);
        Assert.Equal(ProspectCategory.Distributor, prospect.Category); // el botón no lo toca

        var lead = await assertDb.Leads.FirstAsync(l => l.Id == result.Value.LeadId!.Value);
        Assert.Equal(adminUserId, lead.AssignedToUserId);

        var messageResponse = await assertDb.MessageResponses.IgnoreQueryFilters().FirstAsync(r => r.ExternalInboundId == "wh-btn-1");
        Assert.Equal(QuickReplyPayloads.Interested, messageResponse.ButtonPayload);
        Assert.Equal("quick-reply-button-v1", messageResponse.AiModel);
    }

    [Fact]
    public async Task InterestButtonTap_ForWorkshopProspect_RoutesToVentas()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _, ventasUserId) = await SeedOrgWithBothAreasAsync(dbName);
        await SeedProspectWithWhatsappAsync(dbName, orgId, "5491112345678", ProspectCategory.Workshop);

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(
            orgId, "5491112345678", "Estoy interesado", ButtonPayload: QuickReplyPayloads.Interested));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var lead = await assertDb.Leads.FirstAsync(l => l.Id == result.Value.LeadId!.Value);
        Assert.Equal(ventasUserId, lead.AssignedToUserId);
    }

    [Fact]
    public async Task PlainTextInterest_WithoutButtonPayload_RoutesByExistingCategoryTooAndUsesKeywordClassifier()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, adminUserId, _) = await SeedOrgWithBothAreasAsync(dbName);
        await SeedProspectWithWhatsappAsync(dbName, orgId, "5491112345679", ProspectCategory.AutoPartsStore);

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(orgId, "5491112345679", "me interesa, pasame info"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var lead = await assertDb.Leads.FirstAsync(l => l.Id == result.Value.LeadId!.Value);
        Assert.Equal(adminUserId, lead.AssignedToUserId);
    }

    [Fact]
    public async Task InterestButtonTap_NoAdministracionUsers_FallsBackToGeneralRoundRobin()
    {
        var dbName = TestDb.NewDbName();

        int orgId, ventasUserId;
        await using (var db = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
            orgId = org.Id;

            var ventasUser = new User
            {
                OrganizationId = orgId, FirstName = "Vero", LastName = "Ventas", Email = "vero@difrani.com",
                PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Ventas
            };
            db.Users.Add(ventasUser);
            await db.SaveChangesAsync();
            ventasUserId = ventasUser.Id;
        }

        await SeedProspectWithWhatsappAsync(dbName, orgId, "5491112345678", ProspectCategory.Distributor);

        await using var processDb = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(processDb, new KeywordIntentClassifier(), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var result = await service.ProcessAsync(new InboundMessageRequest(
            orgId, "5491112345678", "Estoy interesado", ButtonPayload: QuickReplyPayloads.Interested));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.LeadId);

        // No hay nadie en Administración: el lead nunca debe quedar sin asignar (null), tiene
        // que caer al round-robin general y terminar en el único usuario que existe.
        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var lead = await assertDb.Leads.FirstAsync(l => l.Id == result.Value.LeadId!.Value);
        Assert.Equal(ventasUserId, lead.AssignedToUserId);
    }

    [Fact]
    public async Task UnrecognizedButtonPayload_FallsBackToKeywordClassifier()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _, _) = await SeedOrgWithBothAreasAsync(dbName);
        await SeedProspectWithWhatsappAsync(dbName, orgId, "5491112345678", ProspectCategory.Workshop);

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new InboundMessageService(db, new KeywordIntentClassifier(), new StubMessageProvider(NullLogger<StubMessageProvider>.Instance), new RecordingTelegramNotifier(), NullLogger<InboundMessageService>.Instance);

        var request = new InboundMessageRequest(
            orgId, "5491112345678", "che como andas todo bien", ExternalInboundId: "wh-unknown-btn", ButtonPayload: "unrecognized_payload");
        var result = await service.ProcessAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(IntentClassification.Unclear, result.Value!.Classification);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var messageResponse = await assertDb.MessageResponses.IgnoreQueryFilters().FirstAsync(r => r.ExternalInboundId == "wh-unknown-btn");
        Assert.Equal("unrecognized_payload", messageResponse.ButtonPayload); // se guarda igual, para auditoría
    }
}
