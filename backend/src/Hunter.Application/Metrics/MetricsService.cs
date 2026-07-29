using Hunter.Application.Common;
using Hunter.Application.Metrics.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Crm;
using Hunter.Domain.Prospecting;
using Hunter.Domain.Sales;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Metrics;

public class MetricsService(IHunterDbContext db) : IMetricsService
{
    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var prospectsFound = await db.Prospects.CountAsync(p => !p.IsDeleted, ct);
        var prospectsValid = await db.Prospects.CountAsync(p => !p.IsDeleted && p.Status != ProspectStatus.Invalid, ct);
        var prospectsContacted = await db.Prospects.CountAsync(p => !p.IsDeleted && p.LastContactedAt != null, ct);

        var messagesSent = await db.Messages.CountAsync(m => m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered || m.Status == MessageStatus.Read, ct);
        var responses = await db.MessageResponses.CountAsync(ct);
        var interested = await db.MessageResponses.CountAsync(r => r.Classification == IntentClassification.Interested, ct);
        var leads = await db.Leads.CountAsync(ct);
        var salesWon = await db.Sales.CountAsync(s => s.Status == SaleStatus.Won, ct);
        var salesLost = await db.Leads.CountAsync(l => l.Status == LeadStatus.Lost, ct);

        var revenue = await db.Sales.Where(s => s.Status == SaleStatus.Won).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        var costTotal = await db.Costs.SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        return new DashboardDto(
            prospectsFound,
            prospectsValid,
            prospectsContacted,
            messagesSent,
            responses,
            interested,
            leads,
            salesWon,
            salesLost,
            revenue,
            costTotal,
            Pct(responses, messagesSent),
            Pct(interested, responses),
            Pct(leads, messagesSent),
            Pct(salesWon, messagesSent),
            leads == 0 ? null : Math.Round(costTotal / leads, 2),
            salesWon == 0 ? null : Math.Round(costTotal / salesWon, 2),
            salesWon == 0 ? null : Math.Round(revenue / salesWon, 2));
    }

    public async Task<Result<CampaignMetricsDto>> GetCampaignMetricsAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
            return Result<CampaignMetricsDto>.Failure("Campaña no encontrada.");

        var recipients = await db.CampaignRecipients.CountAsync(r => r.CampaignId == campaignId, ct);
        var sent = await db.CampaignRecipients.CountAsync(r => r.CampaignId == campaignId &&
            (r.Status == CampaignRecipientStatus.Sent || r.Status == CampaignRecipientStatus.Delivered ||
             r.Status == CampaignRecipientStatus.Responded || r.Status == CampaignRecipientStatus.Interested ||
             r.Status == CampaignRecipientStatus.NotInterested), ct);
        var responded = await db.CampaignRecipients.CountAsync(r => r.CampaignId == campaignId &&
            (r.Status == CampaignRecipientStatus.Responded || r.Status == CampaignRecipientStatus.Interested ||
             r.Status == CampaignRecipientStatus.NotInterested), ct);
        var interested = await db.CampaignRecipients.CountAsync(r => r.CampaignId == campaignId && r.Status == CampaignRecipientStatus.Interested, ct);
        var leads = await db.Leads.CountAsync(l => l.CampaignId == campaignId, ct);
        var salesWon = await db.Sales.CountAsync(s => s.CampaignId == campaignId && s.Status == SaleStatus.Won, ct);
        var revenue = await db.Sales.Where(s => s.CampaignId == campaignId && s.Status == SaleStatus.Won).SumAsync(s => (decimal?)s.Amount, ct) ?? 0m;
        var cost = await db.Costs.Where(c => c.CampaignId == campaignId).SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        return Result<CampaignMetricsDto>.Success(new CampaignMetricsDto(
            campaign.Id, campaign.Name, campaign.Status, recipients, sent, responded, interested, leads, salesWon, revenue, cost,
            Pct(responded, sent), Pct(interested, responded), Pct(leads, sent), Pct(salesWon, sent)));
    }

    public async Task<LeadsMetricsDto> GetLeadsMetricsAsync(CancellationToken ct = default)
    {
        var newCount = await db.Leads.CountAsync(l => l.Status == LeadStatus.New, ct);
        var inProgress = await db.Leads.CountAsync(l => l.Status == LeadStatus.InProgress, ct);
        var won = await db.Leads.CountAsync(l => l.Status == LeadStatus.Won, ct);
        var lost = await db.Leads.CountAsync(l => l.Status == LeadStatus.Lost, ct);
        var unattended = await db.Leads.CountAsync(l => l.Status == LeadStatus.New && !l.Activities.Any(), ct);

        var responseTimes = await db.Leads
            .Where(l => l.Activities.Any())
            .Select(l => new { l.CreatedAt, FirstActivityAt = l.Activities.Min(a => a.CreatedAt) })
            .ToListAsync(ct);

        double? avgMinutes = responseTimes.Count == 0
            ? null
            : responseTimes.Average(x => (x.FirstActivityAt - x.CreatedAt).TotalMinutes);

        return new LeadsMetricsDto(newCount, inProgress, won, lost, unattended, avgMinutes);
    }

    public async Task<CostsMetricsDto> GetCostsMetricsAsync(CancellationToken ct = default)
    {
        var byType = await db.Costs
            .GroupBy(c => c.Type)
            .Select(g => new CostByTypeDto(g.Key, g.Sum(c => c.Amount)))
            .ToListAsync(ct);

        var total = byType.Sum(x => x.Amount);

        var prospectsCount = await db.Prospects.CountAsync(p => !p.IsDeleted, ct);
        var messagesSent = await db.Messages.CountAsync(m => m.Status == MessageStatus.Sent || m.Status == MessageStatus.Delivered || m.Status == MessageStatus.Read, ct);
        var leadsCount = await db.Leads.CountAsync(ct);
        var salesWonCount = await db.Sales.CountAsync(s => s.Status == SaleStatus.Won, ct);

        return new CostsMetricsDto(
            total,
            byType,
            prospectsCount == 0 ? null : Math.Round(total / prospectsCount, 2),
            messagesSent == 0 ? null : Math.Round(total / messagesSent, 2),
            leadsCount == 0 ? null : Math.Round(total / leadsCount, 2),
            salesWonCount == 0 ? null : Math.Round(total / salesWonCount, 2));
    }

    private static decimal? Pct(int numerator, int denominator) =>
        denominator == 0 ? null : Math.Round(numerator / (decimal)denominator * 100, 2);
}
