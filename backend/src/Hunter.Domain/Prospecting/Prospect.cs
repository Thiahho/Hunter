using Hunter.Domain.Common;

namespace Hunter.Domain.Prospecting;

public class Prospect : Entity
{
    public int OrganizationId { get; set; }

    public string BusinessName { get; set; } = null!;
    public string? ContactName { get; set; }

    public ProspectCategory Category { get; set; } = ProspectCategory.Unknown;
    public BusinessSize BusinessSize { get; set; } = BusinessSize.Unknown;
    public RecurrencePotential RecurrencePotential { get; set; } = RecurrencePotential.Unknown;
    public DistanceCategory DistanceCategory { get; set; } = DistanceCategory.Unknown;
    public DataQuality DataQuality { get; set; } = DataQuality.D;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Website { get; set; }

    public int? CommercialScore { get; set; }
    public OperationalPriority? OperationalPriority { get; set; }

    public ProspectStatus Status { get; set; } = ProspectStatus.New;

    // Cuántas veces se le mandó un mensaje de reintento automático tras detectar que la
    // respuesta anterior era un auto-responder (ver AutoReplyDetector/InboundMessageService).
    // Tope en AutoReplyFollowUpOptions.MaxAutoReplyAttempts: al llegar ahí se deja de reintentar
    // solo y el prospecto queda para revisión manual.
    public int AutoReplyAttempts { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public DateTimeOffset? LastContactedAt { get; set; }

    public ICollection<ProspectContact> Contacts { get; set; } = new List<ProspectContact>();
    public ICollection<ProspectSource> Sources { get; set; } = new List<ProspectSource>();
    public ICollection<ProspectTag> ProspectTags { get; set; } = new List<ProspectTag>();
}
