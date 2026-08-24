namespace Hunter.Domain.Organizations;

public static class OrganizationSettingsKeys
{
    public const string KillSwitch = "kill_switch";

    // Horas sin actividad en un lead abierto antes de reenviar un recordatorio de escalamiento:
    // ver StaleLeadEscalationBackgroundService.
    public const string StaleLeadEscalationHours = "stale_lead_escalation_hours";

    // Id del archivo de Drive que ProspectDriveSyncService reusa (Files.Update) en cada
    // sincronización en vez de crear uno nuevo cada vez — así el link que el equipo tiene
    // guardado no cambia.
    public const string GoogleDriveProspectsFileId = "google_drive_prospects_file_id";

    // DateTimeOffset.UtcNow (round-trip "O") de la última sincronización exitosa — ver
    // ProspectDriveSyncService.SyncAsync/GetStatusAsync.
    public const string GoogleDriveProspectsSyncedAt = "google_drive_prospects_synced_at";
}
