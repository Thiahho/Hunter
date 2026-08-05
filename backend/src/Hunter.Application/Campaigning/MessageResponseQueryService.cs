using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class MessageResponseQueryService(IHunterDbContext db) : IMessageResponseQueryService
{
    public async Task<PagedResult<MessageResponseDto>> SearchAsync(
        int? campaignId, int? prospectId, IntentClassification? classification, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var responses = db.MessageResponses.AsQueryable();

        if (campaignId is not null)
            responses = responses.Where(r => r.CampaignId == campaignId);
        if (prospectId is not null)
            responses = responses.Where(r => r.ProspectId == prospectId);
        if (classification is not null)
            responses = responses.Where(r => r.Classification == classification);

        var totalItems = await responses.CountAsync(ct);

        var items = await responses
            .OrderByDescending(r => r.ReceivedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MessageResponseDto(
                r.Id, r.ProspectId, r.Prospect.BusinessName, r.CampaignId, r.MessageId, r.Content, r.ReceivedAt,
                r.Classification, r.Confidence, r.ButtonPayload, r.ProcessedAt))
            .ToListAsync(ct);

        return new PagedResult<MessageResponseDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }
}
