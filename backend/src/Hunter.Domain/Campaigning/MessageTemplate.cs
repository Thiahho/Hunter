using Hunter.Domain.Common;

namespace Hunter.Domain.Campaigning;

public class MessageTemplate : Entity
{
    public int OrganizationId { get; set; }

    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public MessagingChannel Channel { get; set; }
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsCatalogTemplate { get; set; }

    // Plantilla usada para el nudge automático de reintento cuando se detecta que el prospecto
    // contestó con un auto-responder (ver AutoReplyDetector). Pensada para "atravesar" el bot del
    // negocio y provocar que atienda un humano, distinta del mensaje de apertura de la campaña.
    public bool IsFollowUpTemplate { get; set; }

    public int? CreatedBy { get; set; }
}
