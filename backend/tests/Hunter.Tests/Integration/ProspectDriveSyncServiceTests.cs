using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Domain.Organizations;
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
}
