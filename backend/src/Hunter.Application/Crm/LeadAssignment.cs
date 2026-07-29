using Hunter.Application.Common;
using Hunter.Domain.Crm;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Crm;

// Asignación round-robin simple (doc 07 Epic09 P1 / doc 14 sección 20): no hay
// distinción por rol ni disponibilidad todavía, solo alterna entre los usuarios
// activos de la organización en orden estable.
public static class LeadAssignment
{
    public static async Task<int?> PickNextAssigneeAsync(IHunterDbContext db, int organizationId, CancellationToken ct = default)
    {
        var userIds = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.IsActive)
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (userIds.Count == 0)
            return null;

        var lastAssigned = await db.Leads
            .IgnoreQueryFilters()
            .Where(l => l.OrganizationId == organizationId && l.AssignedToUserId != null)
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
