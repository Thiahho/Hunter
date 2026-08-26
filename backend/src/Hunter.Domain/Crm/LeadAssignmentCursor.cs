using Hunter.Domain.Identity;

namespace Hunter.Domain.Crm;

// Estado persistido del round-robin de LeadAssignment.PickNextAssigneeAsync, uno por
// (OrganizationId, Area) — Area null representa el pool general (sin filtrar por área). Antes el
// "próximo asignado" se derivaba leyendo el último Lead.AssignedToUserId: dos requests
// concurrentes (ráfaga de mensajes de WhatsApp) podían leer el mismo "último asignado" antes de
// que el primer Lead se guardara y elegir el mismo vendedor, salteando al que le tocaba
// (auditoria.md, hallazgo Medio "race condition en el round-robin"). Version es el token de
// concurrencia optimista: dos requests que intenten avanzar el cursor a partir del mismo estado
// van a chocar en SaveChangesAsync (DbUpdateConcurrencyException) y el perdedor reintenta con una
// lectura fresca, en vez de pisarse silenciosamente.
public class LeadAssignmentCursor
{
    public int Id { get; set; }

    public int OrganizationId { get; set; }
    public UserArea? Area { get; set; }

    public int NextIndex { get; set; }
    public int Version { get; set; }
}
