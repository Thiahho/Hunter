using Hunter.Domain.Prospecting;

namespace Hunter.Domain.Crm;

public class Lead
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }

    public int ProspectId { get; set; }
    public Prospect Prospect { get; set; } = null!;

    public int? CampaignId { get; set; }

    public int? AssignedToUserId { get; set; }

    public LeadStatus Status { get; set; } = LeadStatus.New;
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;
    public LostReason? LostReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    // Último recordatorio de escalamiento mandado por StaleLeadEscalationBackgroundService: evita
    // reenviar el mismo aviso en cada tick mientras el lead siga sin actividad.
    public DateTimeOffset? LastEscalatedAt { get; set; }

    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
}
