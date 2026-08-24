using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// ProspectDriveSyncService.SyncAsync reusa ProspectExportService.ExportAllActiveAsync (ya
// cubierto en ProspectExportServiceTests) — acá se valida específicamente el "siempre el mismo
// archivo": la primera sync crea, guarda el FileId en OrganizationSettings, y la próxima
// sincronización le pasa ESE MISMO Id de vuelta al cliente de Drive (Files.Update, no
// Files.Create) en vez de subir uno nuevo cada vez.
public class ProspectDriveSyncServiceTests
{
    private class FakeGoogleDriveClient : IGoogleDriveClient
    {
        public string? LastExistingFileId { get; private set; }
        public int CallCount { get; private set; }
        public string NextFileId { get; set; } = "drive-file-1";

        public Task<string> UploadOrUpdateAsync(
            string? existingFileId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct = default)
        {
            LastExistingFileId = existingFileId;
            CallCount++;
            return Task.FromResult(NextFileId);
        }
    }

    private static async Task<int> SeedOrgAsync(string dbName)
    {
        await using var db = TestDb.Create(dbName);
        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();
        return org.Id;
    }

    [Fact]
    public async Task SyncAsync_FirstTime_CreatesFileAndPersistsFileId()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var driveClient = new FakeGoogleDriveClient();

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var result = await service.SyncAsync();

        Assert.True(result.Succeeded);
        Assert.Null(driveClient.LastExistingFileId);
        Assert.Equal("drive-file-1", result.Value!.FileId);
        Assert.Equal("https://drive.google.com/file/d/drive-file-1/view", result.Value.DriveUrl);

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var fileIdSetting = await assertDb.OrganizationSettings.FirstAsync(
            s => s.OrganizationId == orgId && s.Key == OrganizationSettingsKeys.GoogleDriveProspectsFileId);
        Assert.Equal("drive-file-1", fileIdSetting.Value);
    }

    [Fact]
    public async Task SyncAsync_SecondTime_PassesTheSamePreviouslyStoredFileId()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var driveClient = new FakeGoogleDriveClient();

        await using (var firstDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var firstService = new ProspectDriveSyncService(
                firstDb, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(firstDb), driveClient);
            await firstService.SyncAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var result = await service.SyncAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(2, driveClient.CallCount);
        Assert.Equal("drive-file-1", driveClient.LastExistingFileId); // el mismo id de la vez anterior, no null
    }

    [Fact]
    public async Task GetStatusAsync_BeforeAnySync_ReturnsNull()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), new FakeGoogleDriveClient());

        var status = await service.GetStatusAsync();

        Assert.Null(status);
    }

    [Fact]
    public async Task GetStatusAsync_AfterSync_ReturnsFileIdAndUrl()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var driveClient = new FakeGoogleDriveClient();

        await using (var syncDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var syncService = new ProspectDriveSyncService(
                syncDb, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(syncDb), driveClient);
            await syncService.SyncAsync();
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var status = await service.GetStatusAsync();

        Assert.NotNull(status);
        Assert.Equal("drive-file-1", status!.FileId);
        Assert.Equal("https://drive.google.com/file/d/drive-file-1/view", status.DriveUrl);
    }

    [Fact]
    public async Task GetStatusAsync_AfterPartialSelectionSync_ReportsTheSyncedCountNotTheLiveTotal()
    {
        // Regresión: el cartel del frontend llegó a mostrar "398 prospectos" (el total activo en
        // vivo) con un archivo de Drive que en realidad solo tenía 20 filas, porque la última
        // sincronización real había sido una selección manual puntual, no la corrida de "todos".
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        var driveClient = new FakeGoogleDriveClient();

        int selectedProspectId;
        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var selected = new Prospect { OrganizationId = orgId, BusinessName = "Seleccionado" };
            var otro1 = new Prospect { OrganizationId = orgId, BusinessName = "Otro 1" };
            var otro2 = new Prospect { OrganizationId = orgId, BusinessName = "Otro 2" };
            seedDb.Prospects.AddRange(selected, otro1, otro2);
            await seedDb.SaveChangesAsync();
            selectedProspectId = selected.Id; // 1 de 3 prospectos activos en total
        }

        await using (var selectionDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var selectionService = new ProspectDriveSyncService(
                selectionDb, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(selectionDb), driveClient);
            await selectionService.SyncSelectionAsync(new ExportProspectsToExcelRequest([selectedProspectId], []));
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var status = await service.GetStatusAsync();

        Assert.NotNull(status);
        Assert.Equal(1, status!.ProspectCount); // lo que de verdad quedó en el archivo, no los 3 activos totales
    }

    [Fact]
    public async Task SyncSelectionAsync_PushesSelectionToTheSameSharedFile()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        int prospectId;
        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Test" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();
            prospectId = prospect.Id;
        }

        var driveClient = new FakeGoogleDriveClient();

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var result = await service.SyncSelectionAsync(new ExportProspectsToExcelRequest([prospectId], []));

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.ProspectCount);
        Assert.Null(driveClient.LastExistingFileId); // primera vez: todavía no había ningún fileId guardado

        await using var assertDb = TestDb.Create(dbName, organizationId: orgId);
        var fileIdSetting = await assertDb.OrganizationSettings.FirstAsync(
            s => s.OrganizationId == orgId && s.Key == OrganizationSettingsKeys.GoogleDriveProspectsFileId);
        Assert.Equal("drive-file-1", fileIdSetting.Value);
    }

    [Fact]
    public async Task SyncSelectionAsync_ThenSyncAsync_ReuseTheSameFileId()
    {
        var dbName = TestDb.NewDbName();
        var orgId = await SeedOrgAsync(dbName);
        int prospectId;
        await using (var seedDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var prospect = new Prospect { OrganizationId = orgId, BusinessName = "Repuestos Test" };
            seedDb.Prospects.Add(prospect);
            await seedDb.SaveChangesAsync();
            prospectId = prospect.Id;
        }

        var driveClient = new FakeGoogleDriveClient();

        await using (var selectionDb = TestDb.Create(dbName, organizationId: orgId))
        {
            var selectionService = new ProspectDriveSyncService(
                selectionDb, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(selectionDb), driveClient);
            await selectionService.SyncSelectionAsync(new ExportProspectsToExcelRequest([prospectId], []));
        }

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var service = new ProspectDriveSyncService(
            db, new FakeCurrentUserService { OrganizationId = orgId }, new ProspectExportService(db), driveClient);

        var result = await service.SyncAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(2, driveClient.CallCount);
        Assert.Equal("drive-file-1", driveClient.LastExistingFileId); // el auto-sync reusa el mismo archivo que ya empujó la selección manual
    }
}
