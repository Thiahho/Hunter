namespace Hunter.Domain.Organizations;

public static class OrganizationSettingsKeys
{
    public const string KillSwitch = "kill_switch";

    // Horas sin actividad en un lead abierto antes de reenviar un recordatorio de escalamiento:
    // ver StaleLeadEscalationBackgroundService.
    public const string StaleLeadEscalationHours = "stale_lead_escalation_hours";
}
