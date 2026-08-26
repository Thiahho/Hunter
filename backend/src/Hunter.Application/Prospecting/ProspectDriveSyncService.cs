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
    // Sin ".xlsx": se sube como Google Sheet nativo (GoogleDriveClient lo convierte al subir), no
    // como un archivo .xlsx real — mantener la extensión en el nombre visible era engañoso.
    private const string FileName = "Prospectos";
    private const string XlsxMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ProspectDriveSyncResultDto>> SyncAsync(CancellationToken ct = default)
    {
        var exportResult = await prospectExportService.ExportAllActiveAsync(ct);
        if (!exportResult.Succeeded)
            return Result<ProspectDriveSyncResultDto>.Failure(exportResult.Error!);

        // ExportAllActiveAsync ya recorrió todos los prospectos activos para armar el workbook, pero
        // no devuelve el conteo — se cuenta acá con la misma condición (!IsDeleted) en vez de tocar
        // el contrato de ExportAllActiveAsync solo para este dato informativo.
        var prospectCount = await db.Prospects.CountAsync(p => !p.IsDeleted, ct);

        return await UploadAndPersistAsync(exportResult.Value!.Content, prospectCount, ct);
    }

    // Botón manual "Exportar a Excel" del listado de prospectos: en vez de descargar un archivo
    // aparte, empuja la selección elegida al MISMO archivo compartido de Drive — mismo mecanismo
    // que SyncAsync (Files.Update sobre el fileId guardado), así el equipo nunca tiene que andar
    // buscando un .xlsx suelto en Descargas. El próximo tick automático (cada 30 min) va a
    // sobreescribirlo igual con todos los prospectos activos — este método sirve para no esperar
    // esos 30 minutos cuando alguien quiere ver ya mismo una selección puntual reflejada ahí.
    public async Task<Result<ProspectDriveSyncResultDto>> SyncSelectionAsync(ExportProspectsToExcelRequest request, CancellationToken ct = default)
    {
        var exportResult = await prospectExportService.ExportAsync(request, ct);
        if (!exportResult.Succeeded)
            return Result<ProspectDriveSyncResultDto>.Failure(exportResult.Error!);

        return await UploadAndPersistAsync(exportResult.Value!.Content, request.ProspectIds.Count, ct);
    }

    private async Task<Result<ProspectDriveSyncResultDto>> UploadAndPersistAsync(byte[] content, int prospectCount, CancellationToken ct)
    {
        var organizationId = currentUser.OrganizationId!.Value;
        var existingFileId = await GetSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsFileId, ct);

        var fileId = await googleDriveClient.UploadOrUpdateAsync(existingFileId, FileName, content, XlsxMimeType, ct);
        if (string.IsNullOrWhiteSpace(fileId))
            return Result<ProspectDriveSyncResultDto>.Failure("Google Drive no devolvió un Id de archivo válido.");

        var syncedAt = DateTimeOffset.UtcNow;
        await UpsertSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsFileId, fileId, ct);
        await UpsertSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsSyncedAt, syncedAt.ToString("O"), ct);
        await UpsertSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsSyncedCount, prospectCount.ToString(), ct);

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

        // Cuenta guardada en el momento de la sync (no el total activo actual): si la última
        // sincronización fue una selección manual puntual (SyncSelectionAsync), el archivo tiene
        // esa cantidad, no el total — mostrar el total en vivo acá era engañoso (el cartel decía
        // "398 prospectos" con un archivo que en realidad solo tenía 20).
        var syncedCountRaw = await GetSettingAsync(organizationId, OrganizationSettingsKeys.GoogleDriveProspectsSyncedCount, ct);
        var prospectCount = int.TryParse(syncedCountRaw, out var parsedCount) ? parsedCount : 0;

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
