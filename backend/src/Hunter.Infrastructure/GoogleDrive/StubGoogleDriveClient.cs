using Hunter.Application.Common;
using Microsoft.Extensions.Logging;

namespace Hunter.Infrastructure.GoogleDrive;

// Se registra cuando GoogleDriveOptions.IsConfigured es false (sin cuenta de servicio/carpeta
// configurada): no rompe ProspectDriveSyncService en dev/sin credenciales, solo loguea y no hace
// ningún llamado real a Drive. Devuelve el mismo id que ya tenía (o "" si nunca sincronizó) para
// que el caller no piense que hubo un archivo nuevo.
public class StubGoogleDriveClient(ILogger<StubGoogleDriveClient> logger) : IGoogleDriveClient
{
    public Task<string> UploadOrUpdateAsync(
        string? existingFileId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct = default)
    {
        logger.LogDebug(
            "[GoogleDrive] Sin configurar (GoogleDrive:ServiceAccountKeyBase64/FolderId) — no se sube \"{FileName}\" ({Bytes} bytes).",
            fileName, content.Length);

        return Task.FromResult(existingFileId ?? string.Empty);
    }
}
