using Hunter.Application.Prospecting.Contracts;

namespace Hunter.Application.Prospecting;

public interface IProspectDuplicateFinder
{
    Task<int?> FindDuplicateProspectIdAsync(
        int organizationId, IReadOnlyCollection<ContactInput> normalizedContacts, CancellationToken ct = default);
}
