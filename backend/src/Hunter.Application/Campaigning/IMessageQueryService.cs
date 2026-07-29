using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageQueryService
{
    Task<PagedResult<MessageDto>> SearchAsync(
        int? campaignId, int? prospectId, MessageStatus? status, int page, int pageSize, CancellationToken ct = default);
}
