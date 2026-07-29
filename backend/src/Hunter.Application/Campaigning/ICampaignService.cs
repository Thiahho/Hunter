using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface ICampaignService
{
    Task<Result<CampaignDto>> CreateAsync(CreateCampaignRequest request, CancellationToken ct = default);
    Task<Result<CampaignDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResult<CampaignListItemDto>> SearchAsync(CampaignStatus? status, int page, int pageSize, CancellationToken ct = default);

    Task<Result<AddRecipientsResultDto>> AddRecipientsAsync(int campaignId, AddRecipientsRequest request, CancellationToken ct = default);
    Task<Result<AddRecipientsResultDto>> AddRecipientsFromSegmentAsync(int campaignId, AddRecipientsFromSegmentRequest request, CancellationToken ct = default);

    Task<Result<bool>> StartAsync(int campaignId, CancellationToken ct = default);
    Task<Result<bool>> PauseAsync(int campaignId, CancellationToken ct = default);
    Task<Result<bool>> CancelAsync(int campaignId, CancellationToken ct = default);

    Task<Result<ProcessQueueResultDto>> ProcessQueueAsync(int campaignId, int batchSize = 50, CancellationToken ct = default);

    Task SetKillSwitchAsync(KillSwitchRequest request, CancellationToken ct = default);
    Task<bool> IsKillSwitchEnabledAsync(CancellationToken ct = default);
}
