using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageResponseQueryService
{
    Task<PagedResult<MessageResponseDto>> SearchAsync(
        string? search, int? campaignId, int? prospectId, IntentClassification? classification, int page, int pageSize, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
}
