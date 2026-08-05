using Hunter.Application.Prospecting.Contracts;

namespace Hunter.Application.Prospecting;

public interface IProspectDuplicateFinder
{
    // businessName/city: segunda señal de duplicado además del contacto exacto (ver
    // ProspectDuplicateFinder). Nulos si el caller no los tiene (ej. import sin ciudad cargada).
    Task<int?> FindDuplicateProspectIdAsync(
        int organizationId, IReadOnlyCollection<ContactInput> normalizedContacts,
        string? businessName, string? city, CancellationToken ct = default);
}
