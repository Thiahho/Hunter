using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageResponseQueryService
{
    Task<PagedResult<MessageResponseDto>> SearchAsync(
        int? campaignId, int? prospectId, IntentClassification? classification, int page, int pageSize, CancellationToken ct = default);
}
