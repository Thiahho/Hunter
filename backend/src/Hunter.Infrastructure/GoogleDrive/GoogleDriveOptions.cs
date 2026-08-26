namespace Hunter.Infrastructure.GoogleDrive;

public class GoogleDriveOptions
{
    public const string SectionName = "GoogleDrive";

    // JSON completo de la clave de la cuenta de servicio (descargada de Google Cloud Console),
    // codificado en base64: un JSON multilínea es frágil como variable de entorno cruda (comillas,
    // saltos de línea), base64 es el estándar para este tipo de credencial.
    public string? ServiceAccountKeyBase64 { get; set; }

    // Carpeta de Drive (compartida de antemano con el email de la cuenta de servicio, permiso
    // Editor) donde vive el archivo sincronizado.
    public string? FolderId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServiceAccountKeyBase64) && !string.IsNullOrWhiteSpace(FolderId);
}
