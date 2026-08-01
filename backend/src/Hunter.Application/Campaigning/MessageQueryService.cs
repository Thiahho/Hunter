using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class MessageQueryService(IHunterDbContext db) : IMessageQueryService
{
    public async Task<PagedResult<MessageDto>> SearchAsync(
        int? campaignId, int? prospectId, MessageStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var messages = db.Messages.AsQueryable();

        if (campaignId is not null)
            messages = messages.Where(m => m.CampaignId == campaignId);
        if (prospectId is not null)
            messages = messages.Where(m => m.ProspectId == prospectId);
        if (status is not null)
            messages = messages.Where(m => m.Status == status);

        var totalItems = await messages.CountAsync(ct);

        var items = await messages
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MessageDto(
                m.Id, m.ProspectId, m.Prospect.BusinessName, m.CampaignId, m.Channel, m.Content, m.Status, m.ExternalMessageId,
                m.SentAt, m.DeliveredAt, m.ReadAt, m.FailedAt, m.FailureReason, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<MessageDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }
}
