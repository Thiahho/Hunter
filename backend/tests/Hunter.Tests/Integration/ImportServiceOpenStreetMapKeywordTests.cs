using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Persistence;
using Hunter.Tests.TestSupport;

namespace Hunter.Tests.Integration;

// Cubre el fix del timeout de Overpass en búsquedas por rubro libre (Keywords): sin radio
// explícito, ImportFromOpenStreetMapAsync tiene que forzar un radio por defecto para evitar caer
// en modo administrativo (OpenStreetMapClient.BuildAreaQuery), que hacía timeout en partidos
// grandes al evaluar un name~regex sin acotar geográficamente primero.
public class ImportServiceOpenStreetMapKeywordTests
{
    private class NullGooglePlacesClient : IGooglePlacesClient
    {
        public Task<IReadOnlyList<GooglePlaceResult>> SearchTextAsync(string query, int maxResults, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<GooglePlaceResult>>([]);
    }

    private class SpyOpenStreetMapClient : IOpenStreetMapClient
    {
        public OpenStreetMapSearchCriteria? LastCriteria { get; private set; }

        public Task<IReadOnlyList<OpenStreetMapPlaceResult>> SearchAsync(OpenStreetMapSearchCriteria criteria, CancellationToken ct = default)
        {
            LastCriteria = criteria;
            return Task.FromResult<IReadOnlyList<OpenStreetMapPlaceResult>>(
                [new OpenStreetMapPlaceResult("node/1", "Peluquería Ana", null, null, null, "123456", ProspectCategory.Unknown)]);
        }
    }

    private static async Task<(HunterDbContext Db, ImportService Service, SpyOpenStreetMapClient Spy)> BuildAsync()
    {
        var dbName = TestDb.NewDbName();
        await using (var seedDb = TestDb.Create(dbName))
        {
            seedDb.Organizations.Add(new Organization { Name = "Difrani" });
            await seedDb.SaveChangesAsync();
        }

        var db = TestDb.Create(dbName, organizationId: 1, userId: 1);
        var spy = new SpyOpenStreetMapClient();
        var service = new ImportService(
            db,
            new FakeCurrentUserService { OrganizationId = 1, UserId = 1 },
            new ProspectDuplicateFinder(db),
            new NullGooglePlacesClient(),
            spy);

        return (db, service, spy);
    }

    [Fact]
    public async Task ImportFromOpenStreetMapAsync_KeywordWithoutRadius_DefaultsToBoundedRadius()
    {
        var (db, service, spy) = await BuildAsync();
        await using var _ = db;

        var request = new OpenStreetMapImportRequest(["Morón"], Keywords: ["Peluquería"]);
        var result = await service.ImportFromOpenStreetMapAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(spy.LastCriteria);
        // No debe quedar en null (modo administrativo/BuildAreaQuery): eso es lo que
        // provocaba el timeout de Overpass en partidos grandes.
        Assert.NotNull(spy.LastCriteria!.RadiusKm);
    }

    [Fact]
    public async Task ImportFromOpenStreetMapAsync_KeywordWithExplicitRadius_KeepsRequestedRadius()
    {
        var (db, service, spy) = await BuildAsync();
        await using var _ = db;

        var request = new OpenStreetMapImportRequest(["Morón"], RadiusKm: 5, Keywords: ["Peluquería"]);
        var result = await service.ImportFromOpenStreetMapAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(5, spy.LastCriteria!.RadiusKm);
    }

    [Fact]
    public async Task ImportFromOpenStreetMapAsync_CategoryOnlyWithoutRadius_KeepsAdministrativeMode()
    {
        var (db, service, spy) = await BuildAsync();
        await using var _ = db;

        var request = new OpenStreetMapImportRequest(["Morón"], Categories: [ProspectCategory.Workshop]);
        var result = await service.ImportFromOpenStreetMapAsync(request);

        Assert.True(result.Succeeded);
        // Comportamiento original sin cambios: sin rubro libre, sin radio explícito = modo
        // administrativo (BuildAreaQuery), que sigue siendo válido para búsquedas por categoría
        // (filtro de tag exacto, indexado, no name~regex).
        Assert.Null(spy.LastCriteria!.RadiusKm);
    }
}
