using Hunter.Application.Crm.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Crm;

public interface ILeadService
{
    Task<PagedResult<LeadListItemDto>> SearchAsync(LeadQuery query, CancellationToken ct = default);
    Task<Result<LeadDto>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Result<bool>> AssignAsync(int id, AssignLeadRequest request, CancellationToken ct = default);
    Task<Result<bool>> SetInProgressAsync(int id, CancellationToken ct = default);

    Task<Result<LeadActivityDto>> AddActivityAsync(int id, CreateLeadActivityRequest request, CancellationToken ct = default);
    Task<Result<FollowUpDto>> AddFollowUpAsync(int id, CreateFollowUpRequest request, CancellationToken ct = default);
    Task<Result<bool>> CompleteFollowUpAsync(int followUpId, CancellationToken ct = default);

    Task<Result<bool>> MarkWonAsync(int id, MarkWonRequest request, CancellationToken ct = default);
    Task<Result<bool>> MarkLostAsync(int id, MarkLostRequest request, CancellationToken ct = default);
}
