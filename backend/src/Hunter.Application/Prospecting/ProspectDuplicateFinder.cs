using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

// Prioridad de deduplicación: mismo canal+valor de contacto dentro de la organización
// (doc 17: ExternalId > Phone > WhatsApp > Email; en V1 el match de ExternalId ocurre
// en el pipeline de importación antes de llegar acá, así que esto cubre el resto).
public class ProspectDuplicateFinder(IHunterDbContext db) : IProspectDuplicateFinder
{
    public async Task<int?> FindDuplicateProspectIdAsync(
        int organizationId, IReadOnlyCollection<ContactInput> normalizedContacts, CancellationToken ct = default)
    {
        foreach (var contact in normalizedContacts)
        {
            var match = await db.ProspectContacts
                .Where(c => c.OrganizationId == organizationId && c.Channel == contact.Channel && c.Value == contact.Value)
                .Select(c => (int?)c.ProspectId)
                .FirstOrDefaultAsync(ct);

            if (match is not null)
                return match;
        }

        return null;
    }
}
