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
    // Reintentos ante choques de concurrencia optimista (dos requests avanzando el cursor al
    // mismo tiempo) o de inserción (dos requests creando el cursor por primera vez a la vez,
    // ver LeadAssignmentCursorConfiguration). Con esta cantidad de reintentos, agotarlos todos
    // requeriría contención sostenida y simultánea de 5+ requests sobre el mismo (org, área)
    // exacto — si igual pasara, se degrada a la política histórica (primer candidato) en vez de
    // fallar la asignación del lead por completo.
    private const int MaxCursorAttempts = 5;

    // Overload sin área: se mantiene para no romper LeadService.cs (asignación manual) ni los
    // tests existentes. Sigue el comportamiento histórico: cualquier usuario activo de la org.
    public static Task<int?> PickNextAssigneeAsync(IHunterDbContext db, int organizationId, CancellationToken ct = default)
        => PickNextAssigneeAsync(db, organizationId, area: null, ct);

    public static async Task<int?> PickNextAssigneeAsync(IHunterDbContext db, int organizationId, UserArea? area, CancellationToken ct = default)
    {
        var candidates = await db.Users
            .IgnoreQueryFilters()
            .Where(u => u.OrganizationId == organizationId && u.IsActive && (area == null || u.Area == area))
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, Notifiable = u.Phone != null || u.TelegramChatId != null })
            .ToListAsync(ct);

        if (candidates.Count == 0)
            return null;

        // Un lead asignado a alguien sin teléfono ni Telegram cargado es un lead que nadie va a
        // ver: mientras haya al menos un usuario notificable en el pool, se reparte solo entre
        // ellos. Si ninguno lo está (todavía nadie cargó sus datos de contacto), se cae al
        // comportamiento histórico -todos los activos- para no dejar el lead sin asignar.
        var notifiableIds = candidates.Where(c => c.Notifiable).Select(c => c.Id).ToList();
        var userIds = notifiableIds.Count > 0 ? notifiableIds : candidates.Select(c => c.Id).ToList();

        return await ClaimNextIndexAsync(db, organizationId, area, userIds, ct);
    }

    // El cursor (uno por org+área) es la única fuente de verdad de "a quién le toca": ya no se
    // deriva del último Lead.AssignedToUserId (auditoria.md, "race condition en el round-robin" —
    // dos requests concurrentes podían leer el mismo "último asignado" antes de que el primer
    // Lead se guardara y elegir el mismo vendedor). Cada intento hace su propia lectura+escritura
    // con token de concurrencia optimista (LeadAssignmentCursor.Version): si otro request avanzó
    // el cursor primero, SaveChangesAsync tira DbUpdateConcurrencyException y se reintenta desde
    // una lectura fresca, en vez de arriesgarse a repetir el mismo assignee.
    private static async Task<int> ClaimNextIndexAsync(
        IHunterDbContext db, int organizationId, UserArea? area, IReadOnlyList<int> userIds, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxCursorAttempts; attempt++)
        {
            var cursor = await db.LeadAssignmentCursors
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Area == area, ct);

            var isNew = cursor is null;
            cursor ??= new LeadAssignmentCursor { OrganizationId = organizationId, Area = area, NextIndex = 0 };

            var assignee = userIds[cursor.NextIndex % userIds.Count];
            cursor.NextIndex = (cursor.NextIndex + 1) % userIds.Count;
            cursor.Version++;

            if (isNew)
                db.LeadAssignmentCursors.Add(cursor);

            try
            {
                await db.SaveChangesAsync(ct);
                return assignee;
            }
            catch (DbUpdateException)
            {
                // Cubre tanto DbUpdateConcurrencyException (choque de Version en un update) como
                // una violación del índice único en un insert (dos requests creando el cursor por
                // primera vez a la vez): en ambos casos, descartar el intento y releer.
                db.Entry(cursor).State = EntityState.Detached;
            }
        }

        // Extremadamente improbable con MaxCursorAttempts reintentos; ante contención sostenida,
        // degradar al primer candidato en vez de dejar el lead sin asignar.
        return userIds[0];
    }
}
