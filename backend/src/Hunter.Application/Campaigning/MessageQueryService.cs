using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class MessageQueryService(IHunterDbContext db) : IMessageQueryService
{
    public async Task<PagedResult<MessageDto>> SearchAsync(
        string? search, int? campaignId, int? prospectId, MessageStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var messages = db.Messages.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            messages = messages.Where(m =>
                m.Prospect.BusinessName.ToLower().Contains(term)
                || m.Prospect.Contacts.Any(c => c.Value.ToLower().Contains(term)));
        }
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
                m.Id, m.ProspectId, m.Prospect.BusinessName,
                m.Prospect.Contacts.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault()
                    ?? m.Prospect.Contacts.Select(c => c.Value).FirstOrDefault(),
                m.CampaignId, m.Channel, m.Content, m.Status, m.ExternalMessageId,
                m.SentAt, m.DeliveredAt, m.ReadAt, m.FailedAt, m.FailureReason, m.Cost, m.Currency, m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<MessageDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }

    public async Task<PagedResult<DailyProspectDto>> SearchDailyAsync(
        string? search, DateOnly? date, string? province, string? city, bool? sent, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var targetDate = date ?? DateOnly.FromDateTime(DateTimeOffset.Now.Date);
        var rangeStart = new DateTimeOffset(targetDate.ToDateTime(TimeOnly.MinValue));
        var rangeEnd = rangeStart.AddDays(1);

        var prospects = db.Prospects.Where(p => !p.IsDeleted && p.CreatedAt >= rangeStart && p.CreatedAt < rangeEnd);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            prospects = prospects.Where(p =>
                p.BusinessName.ToLower().Contains(term)
                || p.Contacts.Any(c => c.Value.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(province))
            prospects = prospects.Where(p => p.Province == province);
        if (!string.IsNullOrWhiteSpace(city))
            prospects = prospects.Where(p => p.City == city);

        var projected = prospects.Select(p => new
        {
            p.Id,
            p.BusinessName,
            p.Province,
            p.City,
            p.CreatedAt,
            Phone = p.Contacts.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault()
                ?? p.Contacts.Select(c => c.Value).FirstOrDefault(),
            Attempts = db.Messages.Count(m => m.ProspectId == p.Id),
            LastMessage = db.Messages.Where(m => m.ProspectId == p.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new { m.Id, m.Status, m.CreatedAt, m.CampaignId, m.CampaignRecipientId })
                .FirstOrDefault(),
            Sent = db.Messages.Any(m => m.ProspectId == p.Id
                && (m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered || m.Status == MessageStatus.Read))
        });

        if (sent is not null)
            projected = projected.Where(x => x.Sent == sent);

        var totalItems = await projected.CountAsync(ct);

        var items = await projected
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DailyProspectDto(
                x.Id,
                x.BusinessName,
                x.Province,
                x.City,
                x.Phone,
                x.CreatedAt,
                x.Sent,
                x.Attempts,
                x.LastMessage != null ? x.LastMessage.CreatedAt : (DateTimeOffset?)null,
                x.LastMessage != null ? x.LastMessage.Status : (MessageStatus?)null,
                x.LastMessage != null ? (x.LastMessage.CampaignRecipientId ?? x.LastMessage.Id) : (int?)null,
                x.LastMessage != null && x.LastMessage.CampaignRecipientId != null,
                x.LastMessage != null ? x.LastMessage.CampaignId : (int?)null))
            .ToListAsync(ct);

        return new PagedResult<DailyProspectDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }

    public async Task<IReadOnlyCollection<FailedContactDto>> GetFailedContactsAsync(
        DateOnly? date, string? province, string? city, CancellationToken ct = default)
    {
        var messages = db.Messages.Where(m => m.Status == MessageStatus.Failed);

        if (date is not null)
        {
            var rangeStart = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue));
            var rangeEnd = rangeStart.AddDays(1);
            messages = messages.Where(m => m.CreatedAt >= rangeStart && m.CreatedAt < rangeEnd);
        }

        if (!string.IsNullOrWhiteSpace(province))
            messages = messages.Where(m => m.Prospect.Province == province);
        if (!string.IsNullOrWhiteSpace(city))
            messages = messages.Where(m => m.Prospect.City == city);

        var rows = await messages
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.ProspectId,
                m.Prospect.BusinessName,
                m.Prospect.Province,
                m.Prospect.City,
                m.FailureReason,
                m.FailedAt,
                m.CreatedAt,
                Phone = m.Prospect.Contacts.Where(c => c.IsPrimary).Select(c => c.Value).FirstOrDefault()
                    ?? m.Prospect.Contacts.Select(c => c.Value).FirstOrDefault()
            })
            .ToListAsync(ct);

        // Un prospecto puede tener varios mensajes fallidos (reintentos); solo interesa la última
        // falla para la lista de "no se pudo contactar".
        return rows
            .GroupBy(r => r.ProspectId)
            .Select(g => g.OrderByDescending(r => r.CreatedAt).First())
            .Select(r => new FailedContactDto(r.ProspectId, r.BusinessName, r.Province, r.City, r.Phone, r.FailureReason, r.FailedAt))
            .ToList();
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (message is null)
            return Result<bool>.Failure("Mensaje no encontrado.");

        db.Messages.Remove(message);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return Result<int>.Success(0);

        var messages = await db.Messages.Where(m => ids.Contains(m.Id)).ToListAsync(ct);
        db.Messages.RemoveRange(messages);
        await db.SaveChangesAsync(ct);

        return Result<int>.Success(messages.Count);
    }
}
