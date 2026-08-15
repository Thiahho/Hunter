using Hunter.Application.Crm;
using Hunter.Application.Crm.Contracts;
using Hunter.Domain.Crm;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Regresión de auditoria.md hallazgo Medio (validación de dinero incompleta en MarkWonAsync) y
// del hallazgo de CreateFollowUpRequest.ScheduledAt sin validar.
public class LeadServiceValidationTests
{
    private static async Task<(int orgId, int leadId)> SeedOpenLeadAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        var lead = new Lead { OrganizationId = org.Id, ProspectId = prospect.Id };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        return (org.Id, lead.Id);
    }

    private static LeadService CreateService(Hunter.Infrastructure.Persistence.HunterDbContext db, int orgId, int userId = 1) =>
        new(db, new FakeCurrentUserService { OrganizationId = orgId, UserId = userId });

    [Theory]
    [InlineData("", 100.0, null)] // moneda vacía
    [InlineData("US", 100.0, null)] // moneda de 2 letras
    [InlineData("ARS", 100.0, -1.0)] // margen negativo
    [InlineData("ARS", 100.0, 150.0)] // margen mayor al monto
    public async Task MarkWonAsync_InvalidMoneyFields_Fails(string currency, double amount, double? margin)
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.MarkWonAsync(
            leadId, new MarkWonRequest((decimal)amount, currency, (decimal?)margin, null));

        Assert.False(result.Succeeded);
        Assert.Empty(db.Sales);
    }

    [Fact]
    public async Task MarkWonAsync_ValidRequest_NormalizesCurrencyAndSucceeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.MarkWonAsync(leadId, new MarkWonRequest(100, "ars", 30, "Frenos"));

        Assert.True(result.Succeeded);
        var sale = await db.Sales.SingleAsync();
        Assert.Equal("ARS", sale.Currency);
        Assert.Equal(30, sale.Margin);
    }

    [Fact]
    public async Task AddFollowUpAsync_PastScheduledAt_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.AddFollowUpAsync(
            leadId, new CreateFollowUpRequest(DateTimeOffset.UtcNow.AddDays(-1), "Llamar de nuevo"));

        Assert.False(result.Succeeded);
        Assert.Empty(db.FollowUps);
    }

    [Fact]
    public async Task AddFollowUpAsync_FutureScheduledAt_Succeeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var result = await service.AddFollowUpAsync(
            leadId, new CreateFollowUpRequest(DateTimeOffset.UtcNow.AddDays(1), "Llamar de nuevo"));

        Assert.True(result.Succeeded);
    }

    // Regresión de auditoria.md hallazgo Bajo (CreateLeadActivityRequest.Description sin límite
    // de longitud). El tope (900) es el largo del mensaje de campaña más largo que manda Difrani hoy.
    [Fact]
    public async Task AddActivityAsync_DescriptionOverLimit_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var tooLong = new string('a', 901);
        var result = await service.AddActivityAsync(leadId, new CreateLeadActivityRequest(LeadActivityType.Note, tooLong));

        Assert.False(result.Succeeded);
        Assert.Empty(db.LeadActivities);
    }

    [Fact]
    public async Task AddActivityAsync_DescriptionAtLimit_Succeeds()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var atLimit = new string('a', 900);
        var result = await service.AddActivityAsync(leadId, new CreateLeadActivityRequest(LeadActivityType.Note, atLimit));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task MarkLostAsync_NotesOverLimit_Fails()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, leadId) = await SeedOpenLeadAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = CreateService(db, orgId);

        var tooLong = new string('a', 901);
        var result = await service.MarkLostAsync(leadId, new MarkLostRequest(LostReason.Other, tooLong));

        Assert.False(result.Succeeded);
        var lead = await db.Leads.FirstAsync(l => l.Id == leadId);
        Assert.Equal(LeadStatus.New, lead.Status); // no se marcó Lost, la validación corta antes
    }
}
