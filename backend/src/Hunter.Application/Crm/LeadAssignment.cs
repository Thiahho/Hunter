using Hunter.Application.Common;
using Hunter.Domain.Crm;
using Hunter.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Crm;

// Asignación round-robin simple (doc 07 Epic09 P1 / doc 14 sección 20): alterna entre los
// usuarios activos de la organización en orden estable. Desde que existe LeadRouting, además
// puede acotarse a un área (ADMINISTRACION/VENTAS) para derivar leads por rubro.
public static class LeadAssignment
{
    // Overload sin área: se mantiene para no romper LeadService.cs (asignación manual) ni los
    // tests existentes. Sigue el comportamiento histórico: cualquier usuario activo de la org.
    public static Task<int?> PickNextAssigneeAsync(IHunterDbContext db, int organizationId, CancellationToken ct = default)
        => PickNextAssigneeAsync(db, organizationId, area: null, ct);

    public static async Task<int?> PickNextAssigneeAsync(IHunterDbContext db, int organizationId, UserArea? area, CancellationToken ct = default)
    {
        var userIds = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.IsActive && (area == null || u.Area == area))
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (userIds.Count == 0)
            return null;

        // El cursor tiene que acotarse al mismo conjunto de candidatos: si no, el "último
        // asignado" de un área casi siempre es un usuario de la otra área, IndexOf da -1 y
        // el round-robin colapsa siempre en el primer usuario de la lista (starvation).
        var lastAssigned = await db.Leads
            .IgnoreQueryFilters()
            .Where(l => l.OrganizationId == organizationId && l.AssignedToUserId != null && userIds.Contains(l.AssignedToUserId.Value))
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => l.AssignedToUserId)
            .FirstOrDefaultAsync(ct);

        if (lastAssigned is null)
            return userIds[0];

        var lastIndex = userIds.IndexOf(lastAssigned.Value);
        var nextIndex = lastIndex < 0 ? 0 : (lastIndex + 1) % userIds.Count;

        return userIds[nextIndex];
    }
}
