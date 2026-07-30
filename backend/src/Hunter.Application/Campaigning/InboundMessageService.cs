using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Crm;
using Hunter.Application.Prospecting;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Crm;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hunter.Application.Campaigning;

public class InboundMessageService(
    IHunterDbContext db,
    IIntentClassifier intentClassifier,
    IMessageProvider messageProvider,
    ILogger<InboundMessageService> logger) : IInboundMessageService
{
    private const decimal ConfidenceThreshold = 0.80m;

    public async Task<Result<InboundMessageResultDto>> ProcessAsync(InboundMessageRequest request, CancellationToken ct = default)
    {
        var organizationExists = await db.Organizations.IgnoreQueryFilters()
            .AnyAsync(o => o.Id == request.OrganizationId, ct);
        if (!organizationExists)
            return Result<InboundMessageResultDto>.Failure("Organización no encontrada.");

        if (!string.IsNullOrWhiteSpace(request.ExternalInboundId))
        {
            var existing = await db.MessageResponses.IgnoreQueryFilters()
                .Where(r => r.OrganizationId == request.OrganizationId && r.ExternalInboundId == request.ExternalInboundId)
                .Select(r => new { r.Id, r.ProspectId, r.Classification, r.Confidence })
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                // Idempotencia (doc 13, sección 12): el mismo evento de webhook ya fue procesado.
                var existingLeadId = await db.Leads.IgnoreQueryFilters()
                    .Where(l => l.OrganizationId == request.OrganizationId && l.ProspectId == existing.ProspectId &&
                                (l.Status == LeadStatus.New || l.Status == LeadStatus.InProgress))
                    .Select(l => (int?)l.Id)
                    .FirstOrDefaultAsync(ct);

                return Result<InboundMessageResultDto>.Success(
                    new InboundMessageResultDto(existing.Id, existing.Classification, existing.Confidence, existingLeadId, false));
            }
        }

        var normalizedContact = ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, request.Contact);

        var prospectContact = await db.ProspectContacts.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == request.OrganizationId && c.Value == normalizedContact)
            .FirstOrDefaultAsync(ct);

        if (prospectContact is null)
            return Result<InboundMessageResultDto>.Failure("No se encontró un prospecto para ese contacto.");

        var prospect = await db.Prospects.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == prospectContact.ProspectId, ct);

        int? campaignId = null;
        int? campaignRecipientId = null;
        int? originatingMessageId = null;

        if (!string.IsNullOrWhiteSpace(request.ExternalMessageId))
        {
            var originatingMessage = await db.Messages.IgnoreQueryFilters()
                .Where(m => m.OrganizationId == request.OrganizationId && m.ExternalMessageId == request.ExternalMessageId)
                .FirstOrDefaultAsync(ct);

            campaignId = originatingMessage?.CampaignId;
            campaignRecipientId = originatingMessage?.CampaignRecipientId;
            originatingMessageId = originatingMessage?.Id;
        }
        else
        {
            var lastMessage = await db.Messages.IgnoreQueryFilters()
                .Where(m => m.OrganizationId == request.OrganizationId && m.ProspectId == prospect.Id)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            campaignId = lastMessage?.CampaignId;
            campaignRecipientId = lastMessage?.CampaignRecipientId;
            originatingMessageId = lastMessage?.Id;
        }

        var classification = await intentClassifier.ClassifyAsync(request.Content, ct);

        var effective = classification.Classification;
        if (effective is IntentClassification.Interested or IntentClassification.Question && classification.Confidence < ConfidenceThreshold)
            effective = IntentClassification.Unclear;

        var messageResponse = new MessageResponse
        {
            OrganizationId = request.OrganizationId,
            ProspectId = prospect.Id,
            CampaignId = campaignId,
            MessageId = originatingMessageId,
            Content = request.Content,
            ReceivedAt = request.ReceivedAt ?? DateTimeOffset.UtcNow,
            Classification = classification.Classification,
            Confidence = classification.Confidence,
            AiModel = classification.ModelName,
            AiPromptVersion = classification.PromptVersion,
            ExternalInboundId = request.ExternalInboundId,
            ProcessedAt = DateTimeOffset.UtcNow
        };
        db.MessageResponses.Add(messageResponse);

        var recipient = campaignRecipientId is null
            ? null
            : await db.CampaignRecipients.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == campaignRecipientId, ct);

        Lead? lead = null;
        var suppressed = false;

        switch (effective)
        {
            case IntentClassification.Stop:
                suppressed = await CreateSuppressionIfNeededAsync(request.OrganizationId, normalizedContact, ct);
                prospect.Status = ProspectStatus.Suppressed;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Stopped;
                break;

            case IntentClassification.NotInterested:
                prospect.Status = ProspectStatus.NotInterested;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.NotInterested;
                break;

            case IntentClassification.Interested:
                if (prospect.Status is not (ProspectStatus.Lead or ProspectStatus.Customer))
                    prospect.Status = ProspectStatus.Lead;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Interested;

                lead = await CreateOrReuseLeadAsync(request.OrganizationId, prospect.Id, campaignId, effective, ct);
                await SendCatalogIfConfiguredAsync(request.OrganizationId, prospect, normalizedContact, ct);
                break;

            case IntentClassification.Question:
                if (prospect.Status is not (ProspectStatus.Lead or ProspectStatus.Customer))
                    prospect.Status = ProspectStatus.Lead;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Interested;

                lead = await CreateOrReuseLeadAsync(request.OrganizationId, prospect.Id, campaignId, effective, ct);
                break;

            default:
                prospect.Status = ProspectStatus.Responded;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Responded;
                break;
        }

        await db.SaveChangesAsync(ct);

        return Result<InboundMessageResultDto>.Success(
            new InboundMessageResultDto(messageResponse.Id, classification.Classification, classification.Confidence, lead?.Id, suppressed));
    }

    private async Task<bool> CreateSuppressionIfNeededAsync(int organizationId, string normalizedContact, CancellationToken ct)
    {
        var alreadySuppressed = await db.Suppressions.IgnoreQueryFilters()
            .AnyAsync(s => s.OrganizationId == organizationId && s.Contact == normalizedContact, ct);

        if (alreadySuppressed)
            return true;

        db.Suppressions.Add(new Suppression
        {
            OrganizationId = organizationId,
            Contact = normalizedContact,
            ContactType = SuppressionContactType.Whatsapp,
            Reason = SuppressionReason.UserRequested,
            Source = "auto_stop_keyword"
        });

        return true;
    }

    // Doc 23, secciones 34-35: solo INTERESTED (no QUESTION, señal de compra más débil)
    // dispara la respuesta automática con el catálogo; el resto sigue derivando a un humano.
    private async Task SendCatalogIfConfiguredAsync(int organizationId, Prospect prospect, string contact, CancellationToken ct)
    {
        var catalogTemplate = await db.MessageTemplates.IgnoreQueryFilters()
            .Where(t => t.OrganizationId == organizationId && t.IsCatalogTemplate && t.IsActive)
            .FirstOrDefaultAsync(ct);

        if (catalogTemplate is null)
        {
            logger.LogWarning(
                "[SendCatalog] Prospecto {ProspectId} marcado Interested pero la organización {OrganizationId} no tiene una plantilla de catálogo activa (IsCatalogTemplate=true). No se envió nada.",
                prospect.Id, organizationId);
            return;
        }

        var content = TemplateRenderer.Render(catalogTemplate.Content, prospect);
        var sendResult = await messageProvider.SendAsync(new SendMessageRequest(MessagingChannel.Whatsapp, contact, content), ct);

        db.Messages.Add(new Message
        {
            OrganizationId = organizationId,
            ProspectId = prospect.Id,
            TemplateId = catalogTemplate.Id,
            Channel = MessagingChannel.Whatsapp,
            Provider = messageProvider.ProviderName,
            Content = content,
            ExternalMessageId = sendResult.ExternalMessageId,
            Status = sendResult.Success ? MessageStatus.Sent : MessageStatus.Failed,
            SentAt = sendResult.Success ? DateTimeOffset.UtcNow : null,
            FailedAt = sendResult.Success ? null : DateTimeOffset.UtcNow
        });
    }

    private async Task<Lead> CreateOrReuseLeadAsync(int organizationId, int prospectId, int? campaignId, IntentClassification classification, CancellationToken ct)
    {
        var openLead = await db.Leads.IgnoreQueryFilters()
            .Where(l => l.OrganizationId == organizationId && l.ProspectId == prospectId &&
                        (l.Status == LeadStatus.New || l.Status == LeadStatus.InProgress))
            .FirstOrDefaultAsync(ct);

        if (openLead is not null)
        {
            openLead.LastActivityAt = DateTimeOffset.UtcNow;
            return openLead;
        }

        var assignee = await LeadAssignment.PickNextAssigneeAsync(db, organizationId, ct);

        var lead = new Lead
        {
            OrganizationId = organizationId,
            ProspectId = prospectId,
            CampaignId = campaignId,
            AssignedToUserId = assignee,
            Status = LeadStatus.New,
            Priority = classification == IntentClassification.Question ? LeadPriority.High : LeadPriority.Medium,
            FirstResponseAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        db.Leads.Add(lead);
        return lead;
    }
}
