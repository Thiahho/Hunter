using Hunter.Domain.Common;
using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Campaigning;

// Envío programado a UN prospecto puntual desde su ficha ("Programar mensaje"): a ScheduledAt,
// el background service renderiza MessageTemplate.Content y lo manda por el mismo camino que
// "Mensaje de prueba" (ITestMessageService.SendAsync), sin pasar por Campaign/CampaignRecipient
// — ver ScheduledMessageService.RunAsync. Deliberadamente sin recurrencia (V1): cada fila es un
// envío puntual, mismo criterio que ScheduledProspectAutomation.
public class ScheduledMessage : Entity
{
    public int OrganizationId { get; set; }

    // Null cuando la fila la crea el sistema (Source = AutoReplyRetry) en vez de un usuario desde
    // "Programar mensaje".
    public int? CreatedByUserId { get; set; }

    // "Manual" (default, cargado a mano desde la ficha del prospecto) o "AutoReplyRetry" (nudge
    // automático tras detectar un auto-responder, ver AutoReplyDetector). Mismo criterio que
    // Suppression.Source: texto libre en vez de enum para no tener que migrar cada vez que se
    // agregue un origen nuevo.
    public string Source { get; set; } = ScheduledMessageSource.Manual;

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int MessageTemplateId { get; set; }
    public MessageTemplate MessageTemplate { get; set; } = null!;

    public DateTimeOffset ScheduledAt { get; set; }
    public ScheduledMessageStatus Status { get; set; } = ScheduledMessageStatus.Pending;

    public DateTimeOffset? RunAt { get; set; }
    public int? MessageId { get; set; }
    public string? FailureReason { get; set; }
}

public static class ScheduledMessageSource
{
    public const string Manual = "Manual";
    public const string AutoReplyRetry = "AutoReplyRetry";
}

public enum ScheduledMessageStatus
{
    Pending,
    Running,
    Sent,
    Failed,
    Cancelled
}
