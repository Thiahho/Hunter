using System.Text;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Organizations;
using Hunter.Infrastructure.Persistence;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Cubre el nuevo ConfirmAsync(batchId, ConfirmImportRequest?): la selección por fila que la
// pantalla de búsqueda OSM necesita, sin romper el modo "confirmar todo" que sigue usando CSV.
public class ImportServiceConfirmSelectionTests
{
    private class NullGooglePlacesClient : IGooglePlacesClient
    {
        public Task<IReadOnlyList<GooglePlaceResult>> SearchTextAsync(string query, int maxResults, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<GooglePlaceResult>>([]);
    }

    private class NullOpenStreetMapClient : IOpenStreetMapClient
    {
        public Task<IReadOnlyList<OpenStreetMapPlaceResult>> SearchAsync(OpenStreetMapSearchCriteria criteria, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpenStreetMapPlaceResult>>([]);
    }

    private static ImportService BuildService(HunterDbContext db, int organizationId) => new(
        db,
        new FakeCurrentUserService { OrganizationId = organizationId, UserId = 1 },
        new ProspectDuplicateFinder(db),
        new NullGooglePlacesClient(),
        new NullOpenStreetMapClient());

    private static async Task<(string DbName, int OrgId)> SeedOrganizationAsync()
    {
        var dbName = TestDb.NewDbName();
        await using var seedDb = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        seedDb.Organizations.Add(org);
        await seedDb.SaveChangesAsync();
        return (dbName, org.Id);
    }

    private static MemoryStream ToCsvStream(string csv) => new(Encoding.UTF8.GetBytes(csv));

    [Fact]
    public async Task ConfirmAsync_WithSelectedRecordIds_OnlyImportsSelectedRows()
    {
        var (dbName, orgId) = await SeedOrganizationAsync();
        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = BuildService(db, orgId);

        const string csv = "business_name,whatsapp\nNegocio Uno,5491111111111\nNegocio Dos,5491122222222\nNegocio Tres,5491133333333\n";
        await using var stream = ToCsvStream(csv);

        var preview = await service.ImportCsvAsync(stream, "prospectos.csv");
        Assert.True(preview.Succeeded);
        Assert.Equal(3, preview.Value!.ValidRecords);

        var records = await service.GetRecordsAsync(preview.Value.BatchId);
        Assert.True(records.Succeeded);
        var selectedIds = records.Value!.Where(r => r.BusinessName != "Negocio Dos").Select(r => r.Id).ToList();
        Assert.Equal(2, selectedIds.Count);

        var confirm = await service.ConfirmAsync(preview.Value.BatchId, new ConfirmImportRequest(selectedIds));

        Assert.True(confirm.Succeeded);
        Assert.Equal(2, confirm.Value!.Created);

        var prospectNames = await db.Prospects.Select(p => p.BusinessName).ToListAsync();
        Assert.Equal(2, prospectNames.Count);
        Assert.DoesNotContain("Negocio Dos", prospectNames);
    }

    [Fact]
    public async Task ConfirmAsync_WithoutSelectedRecordIds_ImportsAllValidRows()
    {
        var (dbName, orgId) = await SeedOrganizationAsync();
        await using var db = TestDb.Create(dbName, organizationId: orgId, userId: 1);
        var service = BuildService(db, orgId);

        const string csv = "business_name,whatsapp\nNegocio Uno,5491111111111\nNegocio Dos,5491122222222\n";
        await using var stream = ToCsvStream(csv);

        var preview = await service.ImportCsvAsync(stream, "prospectos.csv");
        Assert.True(preview.Succeeded);

        var confirm = await service.ConfirmAsync(preview.Value!.BatchId);

        Assert.True(confirm.Succeeded);
        Assert.Equal(2, confirm.Value!.Created);
    }
}
