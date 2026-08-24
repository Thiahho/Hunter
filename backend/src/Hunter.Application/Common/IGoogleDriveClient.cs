namespace Hunter.Application.Common;

// No específico de prospectos a propósito (vive en Common, no en Prospecting): es una
// abstracción genérica de "subir o actualizar un archivo en una carpeta de Drive", reusable por
// cualquier otra sincronización futura.
public interface IGoogleDriveClient
{
    // Si existingFileId viene y todavía existe en Drive, actualiza ESE archivo (Files.Update) —
    // el link de Drive no cambia entre sincronizaciones. Si es null, vacío, o ya no existe (lo
    // borraron a mano), crea uno nuevo en la carpeta configurada (Files.Create) y devuelve su Id
    // para que el caller lo persista y lo reuse la próxima vez.
    Task<string> UploadOrUpdateAsync(
        string? existingFileId, string fileName, byte[] content, string sourceMimeType, CancellationToken ct = default);
}
