using Hunter.Application.Common;
using Hunter.Application.Crm.Contracts;
using Hunter.Domain.Crm;
using Hunter.Domain.Prospecting;
using Hunter.Domain.Sales;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Crm;

public class LeadService(IHunterDbContext db, ICurrentUserService currentUser) : ILeadService
{
    public async Task<PagedResult<LeadListItemDto>> SearchAsync(LeadQuery query, CancellationToken ct = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var leads = db.Leads.AsQueryable();

        if (query.Status is not null)
            leads = leads.Where(l => l.Status == query.Status);
        if (query.Priority is not null)
            leads = leads.Where(l => l.Priority == query.Priority);
        if (query.AssignedUserId is not null)
            leads = leads.Where(l => l.AssignedToUserId == query.AssignedUserId);
        if (query.CampaignId is not null)
            leads = leads.Where(l => l.CampaignId == query.CampaignId);

        var totalItems = await leads.CountAsync(ct);

        var items = await leads
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LeadListItemDto(l.Id, l.ProspectId, l.Prospect.BusinessName, l.Status, l.Priority, l.AssignedToUserId, l.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<LeadListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }

    public async Task<Result<LeadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = await LoadDtoAsync(id, ct);
        return dto is null ? Result<LeadDto>.Failure("Lead no encontrado.") : Result<LeadDto>.Success(dto);
    }

    public async Task<Result<bool>> AssignAsync(int id, AssignLeadRequest request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<bool>.Failure("Lead no encontrado.");

        lead.AssignedToUserId = request.UserId ?? await LeadAssignment.PickNextAssigneeAsync(db, currentUser.OrganizationId!.Value, ct);
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> SetInProgressAsync(int id, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<bool>.Failure("Lead no encontrado.");

        if (lead.Status is not (LeadStatus.New or LeadStatus.InProgress))
            return Result<bool>.Failure($"El lead está en estado {lead.Status}, no se puede pasar a en proceso.");

        lead.Status = LeadStatus.InProgress;
        lead.LastActivityAt = DateTimeOffset.UtcNow;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<LeadActivityDto>> AddActivityAsync(int id, CreateLeadActivityRequest request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<LeadActivityDto>.Failure("Lead no encontrado.");

        var activity = new LeadActivity
        {
            LeadId = lead.Id,
            UserId = currentUser.UserId!.Value,
            Type = request.Type,
            Description = request.Description
        };

        db.LeadActivities.Add(activity);
        lead.LastActivityAt = DateTimeOffset.UtcNow;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<LeadActivityDto>.Success(new LeadActivityDto(activity.Id, activity.Type, activity.Description, activity.UserId, activity.CreatedAt));
    }

    public async Task<Result<FollowUpDto>> AddFollowUpAsync(int id, CreateFollowUpRequest request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<FollowUpDto>.Failure("Lead no encontrado.");

        var followUp = new FollowUp
        {
            LeadId = lead.Id,
            UserId = currentUser.UserId!.Value,
            ScheduledAt = request.ScheduledAt,
            Notes = request.Notes
        };

        db.FollowUps.Add(followUp);
        await db.SaveChangesAsync(ct);

        return Result<FollowUpDto>.Success(new FollowUpDto(followUp.Id, followUp.ScheduledAt, followUp.Status, followUp.Notes, followUp.CompletedAt));
    }

    public async Task<Result<bool>> CompleteFollowUpAsync(int followUpId, CancellationToken ct = default)
    {
        var followUp = await db.FollowUps.FirstOrDefaultAsync(f => f.Id == followUpId, ct);
        if (followUp is null)
            return Result<bool>.Failure("Seguimiento no encontrado.");

        followUp.Status = Domain.Crm.FollowUpStatus.Completed;
        followUp.CompletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> MarkWonAsync(int id, MarkWonRequest request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<bool>.Failure("Lead no encontrado.");

        if (lead.Status is LeadStatus.Won or LeadStatus.Lost)
            return Result<bool>.Failure($"El lead ya está en estado {lead.Status}.");

        var prospect = await db.Prospects.FirstOrDefaultAsync(p => p.Id == lead.ProspectId, ct);
        if (prospect is null)
            return Result<bool>.Failure("Prospecto no encontrado.");

        lead.Status = LeadStatus.Won;
        lead.ClosedAt = DateTimeOffset.UtcNow;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        prospect.Status = ProspectStatus.Customer;

        db.Sales.Add(new Sale
        {
            OrganizationId = currentUser.OrganizationId!.Value,
            LeadId = lead.Id,
            CampaignId = lead.CampaignId,
            ProspectId = lead.ProspectId,
            SellerId = currentUser.UserId!.Value,
            Amount = request.Amount,
            Currency = request.Currency,
            Margin = request.Margin,
            ProductCategory = request.ProductCategory,
            Status = SaleStatus.Won
        });

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> MarkLostAsync(int id, MarkLostRequest request, CancellationToken ct = default)
    {
        var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lead is null)
            return Result<bool>.Failure("Lead no encontrado.");

        if (lead.Status is LeadStatus.Won or LeadStatus.Lost)
            return Result<bool>.Failure($"El lead ya está en estado {lead.Status}.");

        lead.Status = LeadStatus.Lost;
        lead.LostReason = request.LostReason;
        lead.ClosedAt = DateTimeOffset.UtcNow;
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            db.LeadActivities.Add(new LeadActivity
            {
                LeadId = lead.Id,
                UserId = currentUser.UserId!.Value,
                Type = Domain.Crm.LeadActivityType.Note,
                Description = request.Notes
            });
        }

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private async Task<LeadDto?> LoadDtoAsync(int id, CancellationToken ct)
    {
        return await db.Leads
            .Where(l => l.Id == id)
            .Select(l => new LeadDto(
                l.Id,
                l.ProspectId,
                l.Prospect.BusinessName,
                l.CampaignId,
                l.AssignedToUserId,
                l.Status,
                l.Priority,
                l.LostReason,
                l.CreatedAt,
                l.FirstResponseAt,
                l.LastActivityAt,
                l.ClosedAt,
                l.Activities.OrderByDescending(a => a.CreatedAt)
                    .Select(a => new LeadActivityDto(a.Id, a.Type, a.Description, a.UserId, a.CreatedAt)).ToList(),
                l.FollowUps.OrderBy(f => f.ScheduledAt)
                    .Select(f => new FollowUpDto(f.Id, f.ScheduledAt, f.Status, f.Notes, f.CompletedAt)).ToList()))
            .FirstOrDefaultAsync(ct);
    }
}
