using Hunter.Application.Common;
using Hunter.Application.Prospecting.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Prospecting;

// Prioridad de deduplicación: mismo canal+valor de contacto dentro de la organización
// (doc 17: ExternalId > Phone > WhatsApp > Email; en V1 el match de ExternalId ocurre
// en el pipeline de importación antes de llegar acá, así que esto cubre el resto).
// Si no hay contacto que matchee exacto, se reintenta por nombre de negocio + ciudad (ver
// FindByNameAndCityAsync): cubre el caso de un mismo comercio que reaparece en una búsqueda
// nueva con el teléfono mal formateado, desactualizado, o cargado en otro canal.
public class ProspectDuplicateFinder(IHunterDbContext db) : IProspectDuplicateFinder
{
    public async Task<int?> FindDuplicateProspectIdAsync(
        int organizationId, IReadOnlyCollection<ContactInput> normalizedContacts,
        string? businessName, string? city, CancellationToken ct = default)
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

        return await FindByNameAndCityAsync(organizationId, businessName, city, ct);
    }

    // Comparación case/espacios-insensible: "Repuestos Oeste" en Overpass y " repuestos oeste "
    // en una carga manual tienen que matchear igual. Requiere AMBOS campos (nombre y ciudad) no
    // vacíos: nombre solo sería demasiado agresivo (cadenas/franquicias con sucursales en varias
    // ciudades, ej. dos "Bridgestone" distintos).
    private async Task<int?> FindByNameAndCityAsync(int organizationId, string? businessName, string? city, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(businessName) || string.IsNullOrWhiteSpace(city))
            return null;

        var normalizedName = businessName.Trim().ToLowerInvariant();
        var normalizedCity = city.Trim().ToLowerInvariant();

        return await db.Prospects
            .Where(p => p.OrganizationId == organizationId
                && p.BusinessName.ToLower() == normalizedName
                && p.City != null && p.City.ToLower() == normalizedCity)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(ct);
    }
}
