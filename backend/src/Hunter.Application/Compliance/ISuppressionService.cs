using Hunter.Application.Compliance.Contracts;
using Hunter.Domain.Compliance;
using Hunter.Shared;

namespace Hunter.Application.Compliance;

public interface ISuppressionService
{
    Task<Result<SuppressionDto>> CreateAsync(CreateSuppressionRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<SuppressionDto>> ListAsync(CancellationToken ct = default);
    Task<bool> IsSuppressedAsync(SuppressionContactType contactType, string rawContact, CancellationToken ct = default);
}
