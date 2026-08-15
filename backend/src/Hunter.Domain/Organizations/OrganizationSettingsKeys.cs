namespace Hunter.Domain.Organizations;

public static class OrganizationSettingsKeys
{
    public const string KillSwitch = "kill_switch";

    // Cantidad máxima de leads abiertos (New/InProgress) sin actividad reciente que se toleran
    // antes de pausar el envío automático de campañas de prospección en frío: ver
    // ScheduledProspectAutomationService.RunAsync.
    public const string OpenLeadBacklogThreshold = "open_lead_backlog_threshold";

    // Horas sin actividad en un lead abierto antes de reenviar un recordatorio de escalamiento:
    // ver StaleLeadEscalationBackgroundService.
    public const string StaleLeadEscalationHours = "stale_lead_escalation_hours";
}
