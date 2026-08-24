using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Hunter.Domain.Organizations;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

// Reemplaza "descargar y resubir a mano" por un único archivo que se mantiene actualizado en
// Drive: arma el Excel de TODOS los prospectos activos (ver ProspectExportService.ExportAllActiveAsync)
// y lo sube/actualiza SIEMPRE al mismo archivo (Files.Update sobre el fileId guardado la vez
// anterior), para que el link que el equipo tiene guardado nunca cambie.
public class ProspectDriveSyncService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    IProspectExportService prospectExportService,
    IGoogleDriveClient googleDriveClient) : IProspectDriveSyncService
{
    private const string FileName = "Prospectos.xlsx";
    private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ProspectDriveSyncResultDto>> SyncAsync(CancellationToken ct = default)
    {
        var exportResult = await prospectExportService.ExportAllActiveAsync(ct);
        if (!exportResult.Succeeded)
            return Result<ProspectDriveSyncResultDto>.Failure(exportResult.Error!);

        var organizationId = currentUser.OrganizationId!.Value;
        var existingFileId = await GetSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsFileId, ct);

        var fileId = await googleDriveClient.UploadOrUpdateAsync(
            existingFileId, FileName, exportResult.Value!.Content, XlsxMimeType, ct);

        if (string.IsNullOrWhiteSpace(fileId))
            return Result<ProspectDriveSyncResultDto>.Failure("Google Drive no devolvió un Id de archivo válido.");

        var syncedAt = DateTimeOffset.UtcNow;
        await UpsertSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsFileId, fileId, ct);
        await UpsertSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsSyncedAt, syncedAt.ToString("O"), ct);

        // ExportAllActiveAsync ya recorrió todos los prospectos activos para armar el workbook, pero
        // no devuelve el conteo — se cuenta acá con la misma condición (!IsDeleted) en vez de tocar
        // el contrato de ExportAllActiveAsync solo para este dato informativo.
        var prospectCount = await db.Prospects.CountAsync(p => !p.IsDeleted, ct);

        return Result<ProspectDriveSyncResultDto>.Success(new ProspectDriveSyncResultDto(fileId, BuildDriveUrl(fileId), syncedAt, prospectCount));
    }

    public async Task<ProspectDriveSyncResultDto?> GetStatusAsync(CancellationToken ct = default)
    {
        var organizationId = currentUser.OrganizationId!.Value;

        var fileId = await GetSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsFileId, ct);
        if (string.IsNullOrWhiteSpace(fileId))
            return null;

        var syncedAtRaw = await GetSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsSyncedAt, ct);
        var syncedAt = DateTimeOffset.TryParse(syncedAtRaw, out var parsed) ? parsed : (DateTimeOffset?)null;
        var prospectCount = await db.Prospects.CountAsync(p => !p.IsDeleted, ct);

        return new ProspectDriveSyncResultDto(fileId, BuildDriveUrl(fileId), syncedAt ?? DateTimeOffset.MinValue, prospectCount);
    }

    private static string BuildDriveUrl(string fileId) => $"https://drive.google.com/file/d/{fileId}/view";

    private async Task<string?> GetSettingAsync(int organizationId, string key, CancellationToken ct) =>
        await db.OrganizationSettings
            .Where(s => s.OrganizationId == organizationId && s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

    private async Task UpsertSettingAsync(int organizationId, string key, string value, CancellationToken ct)
    {
        var setting = await db.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Key == key, ct);

        if (setting is null)
        {
            db.OrganizationSettings.Add(new OrganizationSettings { OrganizationId = organizationId, Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
