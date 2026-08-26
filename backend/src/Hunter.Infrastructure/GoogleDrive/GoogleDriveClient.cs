using System.Net;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Hunter.Application.Common;
using Microsoft.Extensions.Options;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace Hunter.Infrastructure.GoogleDrive;

// Cliente de Google Drive API v3 autenticado con una cuenta de servicio (sin login/consentimiento
// de por medio): el acceso al archivo lo da compartir la carpeta de destino con el email de esa
// cuenta, no un rol de IAM del proyecto — ver la guía de configuración en docs/12.
public class GoogleDriveClient(IOptions<GoogleDriveOptions> options) : IGoogleDriveClient
{
    // Convierte a Google Sheets nativo al subir (Body.MimeType = spreadsheet, contenido con el
    // mimeType real del .xlsx): así el archivo se abre directo en el navegador — filtra, se
    // comenta, se ve desde el celular — en vez de tener que descargarlo cada vez, que es el punto
    // de "que se comparta" en Drive.
    private const string GoogleSheetsMimeType = "application/vnd.google-apps.spreadsheet";

    public async Task<string> UploadOrUpdateAsync(
        string? existingFileId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct = default)
    {
        var opts = options.Value;
        var keyBytes = Convert.FromBase64String(opts.ServiceAccountKeyBase64!);
        // ServiceAccountCredential.FromServiceAccountData + ToGoogleCredential(), no
        // GoogleCredential.FromJson/FromStream: esas dos están obsoletas en Google.Apis.Auth
        // (riesgo de seguridad si el JSON queda flotando en memoria más tiempo del necesario).
        using var keyStream = new MemoryStream(keyBytes);
        var credential = ServiceAccountCredential.FromServiceAccountData(keyStream)
            .ToGoogleCredential()
            .CreateScoped(DriveService.Scope.Drive);

        using var service = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Hunter CRM"
        });

        if (!string.IsNullOrWhiteSpace(existingFileId))
        {
            var updatedId = await TryUpdateAsync(service, existingFileId, fileName, content, sourceMimeType, ct);
            if (updatedId is not null)
                return updatedId;
            // existingFileId ya no existe (404: lo borraron a mano en Drive) — sigue abajo.
        }

        // Antes de crear: buscar si ya hay un archivo con este nombre en la carpeta. Importa
        // porque una cuenta de servicio NO tiene cuota propia de Drive en una cuenta personal de
        // Google (0 bytes): crear un archivo nuevo como la cuenta de servicio falla con
        // "storageQuotaExceeded", aunque la carpeta esté compartida con permiso de Editor —
        // actualizar el CONTENIDO de un archivo que ya existe (con un dueño real, con cuota) sí
        // funciona. Si alguien dejó un archivo placeholder con este nombre en la carpeta (a mano,
        // ver la guía de configuración), se reusa acá en vez de intentar crear uno nuevo.
        var foundId = await FindByNameAsync(service, opts.FolderId!, fileName, ct);
        if (foundId is not null)
        {
            var updatedId = await TryUpdateAsync(service, foundId, fileName, content, sourceMimeType, ct);
            if (updatedId is not null)
                return updatedId;
        }

        return await CreateAsync(service, opts.FolderId!, fileName, content, sourceMimeType, ct);
    }

    private static async Task<string?> FindByNameAsync(DriveService service, string folderId, string fileName, CancellationToken ct)
    {
        var request = service.Files.List();
        request.Q = $"name = '{fileName}' and '{folderId}' in parents and trashed = false";
        request.Fields = "files(id)";
        request.PageSize = 1;

        var result = await request.ExecuteAsync(ct);
        return result.Files?.FirstOrDefault()?.Id;
    }

    private static async Task<string> CreateAsync(
        DriveService service, string folderId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct)
    {
        var metadata = new DriveFile
        {
            Name = fileName,
            MimeType = GoogleSheetsMimeType,
            Parents = [folderId]
        };

        using var stream = new MemoryStream(content);
        var request = service.Files.Create(metadata, stream, sourceMimeType);
        request.Fields = "id";

        var progress = await request.UploadAsync(ct);
        if (progress.Status != UploadStatus.Completed)
            throw new InvalidOperationException($"No se pudo crear el archivo en Drive: {progress.Exception?.Message}");

        return request.ResponseBody.Id;
    }

    private static async Task<string?> TryUpdateAsync(
        DriveService service, string fileId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct)
    {
        var metadata = new DriveFile { Name = fileName };

        using var stream = new MemoryStream(content);
        var request = service.Files.Update(metadata, fileId, stream, sourceMimeType);
        request.Fields = "id";

        try
        {
            var progress = await request.UploadAsync(ct);
            if (progress.Status != UploadStatus.Completed)
                throw new InvalidOperationException($"No se pudo actualizar el archivo en Drive: {progress.Exception?.Message}");

            return request.ResponseBody.Id;
        }
        catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
