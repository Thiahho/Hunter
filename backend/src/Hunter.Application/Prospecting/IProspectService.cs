using Hunter.Application.Prospecting.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Prospecting;

public interface IProspectService
{
    Task<Result<ProspectDto>> CreateAsync(CreateProspectRequest request, CancellationToken ct = default);
    Task<Result<ProspectDto>> UpdateAsync(int id, UpdateProspectRequest request, CancellationToken ct = default);
    Task<Result<ProspectDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<ProspectListItemDto>> SearchAsync(ProspectQuery query, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<bool>> AddTagAsync(int prospectId, string tagName, CancellationToken ct = default);
    Task<Result<bool>> RemoveTagAsync(int prospectId, int tagId, CancellationToken ct = default);
}
