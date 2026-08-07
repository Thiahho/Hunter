using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Compliance;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class CampaignService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    ISuppressionService suppressionService,
    IMessageProvider messageProvider) : ICampaignService
{
    private const string KillSwitchKey = OrganizationSettingsKeys.KillSwitch;

    public async Task<Result<CampaignDto>> CreateAsync(CreateCampaignRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<CampaignDto>.Failure("El nombre de la campaña es obligatorio.");

        var template = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == request.MessageTemplateId, ct);
        if (template is null)
            return Result<CampaignDto>.Failure("La plantilla indicada no existe.");
        if (!template.IsActive)
            return Result<CampaignDto>.Failure("La plantilla indicada no está activa.");

        var campaign = new Campaign
        {
            OrganizationId = currentUser.OrganizationId!.Value,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Channel = request.Channel,
            MessageTemplateId = request.MessageTemplateId,
            MaxMessages = request.MaxMessages,
            MessagesPerMinute = request.MessagesPerMinute,
            MessagesPerHour = request.MessagesPerHour,
            MessagesPerDay = request.MessagesPerDay,
            CreatedBy = currentUser.UserId
        };

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);

        return Result<CampaignDto>.Success(await LoadDtoAsync(campaign.Id, ct) ?? throw new InvalidOperationException());
    }

    public async Task<Result<CampaignDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = await LoadDtoAsync(id, ct);
        return dto is null ? Result<CampaignDto>.Failure("Campaña no encontrada.") : Result<CampaignDto>.Success(dto);
    }

    public async Task<PagedResult<CampaignListItemDto>> SearchAsync(CampaignStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var campaigns = db.Campaigns.AsQueryable();
        if (status is not null)
            campaigns = campaigns.Where(c => c.Status == status);

        var totalItems = await campaigns.CountAsync(ct);

        var items = await campaigns
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignListItemDto(
                c.Id,
                c.Name,
                c.Status,
                c.Channel,
                c.MessageTemplate.Name,
                c.MessagesPerMinute,
                c.Recipients.Count,
                c.Recipients.Count(r => r.Status != CampaignRecipientStatus.Pending && r.Status != CampaignRecipientStatus.Queued),
                c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CampaignListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
    }

    public async Task<Result<AddRecipientsResultDto>> AddRecipientsAsync(int campaignId, AddRecipientsRequest request, CancellationToken ct = default)
    {
        var campaign = await GetEditableCampaignAsync(campaignId, ct);
        if (campaign is null)
            return Result<AddRecipientsResultDto>.Failure("Campaña no encontrada o no admite cambios en sus destinatarios.");

        return Result<AddRecipientsResultDto>.Success(await AddRecipientsInternalAsync(campaign, request.ProspectIds, ct));
    }

    public async Task<Result<AddRecipientsResultDto>> AddRecipientsFromSegmentAsync(int campaignId, AddRecipientsFromSegmentRequest request, CancellationToken ct = default)
    {
        var campaign = await GetEditableCampaignAsync(campaignId, ct);
        if (campaign is null)
            return Result<AddRecipientsResultDto>.Failure("Campaña no encontrada o no admite cambios en sus destinatarios.");

        var prospects = db.Prospects.Where(p => !p.IsDeleted);

        if (request.Category is not null)
            prospects = prospects.Where(p => p.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.City))
            prospects = prospects.Where(p => p.City == request.City);
        if (!string.IsNullOrWhiteSpace(request.Province))
            prospects = prospects.Where(p => p.Province == request.Province);
        if (request.BusinessSize is not null)
            prospects = prospects.Where(p => p.BusinessSize == request.BusinessSize);
        if (!string.IsNullOrWhiteSpace(request.Tag))
            prospects = prospects.Where(p => p.ProspectTags.Any(pt => pt.Tag.Name == request.Tag));

        var prospectIds = await prospects.Select(p => p.Id).ToListAsync(ct);

        return Result<AddRecipientsResultDto>.Success(await AddRecipientsInternalAsync(campaign, prospectIds, ct));
    }

    public async Task<Result<bool>> StartAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.Include(c => c.MessageTemplate).FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
            return Result<bool>.Failure("Campaña no encontrada.");

        if (campaign.Status is not (CampaignStatus.Draft or CampaignStatus.Ready or CampaignStatus.Paused))
            return Result<bool>.Failure($"La campaña está en estado {campaign.Status}, no se puede iniciar.");

        if (!campaign.MessageTemplate.IsActive)
            return Result<bool>.Failure("La plantilla de la campaña ya no está activa.");

        var hasRecipients = await db.CampaignRecipients.AnyAsync(r => r.CampaignId == campaignId, ct);
        if (!hasRecipients)
            return Result<bool>.Failure("La campaña no tiene destinatarios.");

        if (await IsKillSwitchEnabledAsync(ct))
            return Result<bool>.Failure("El Kill Switch está activo: no se pueden iniciar campañas.");

        campaign.Status = CampaignStatus.Running;
        campaign.StartDate ??= DateTimeOffset.UtcNow;
        campaign.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> PauseAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
            return Result<bool>.Failure("Campaña no encontrada.");

        if (campaign.Status != CampaignStatus.Running)
            return Result<bool>.Failure($"La campaña está en estado {campaign.Status}, no se puede pausar.");

        campaign.Status = CampaignStatus.Paused;
        campaign.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CancelAsync(int campaignId, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
            return Result<bool>.Failure("Campaña no encontrada.");

        if (campaign.Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
            return Result<bool>.Failure($"La campaña ya está en estado {campaign.Status}.");

        campaign.Status = CampaignStatus.Cancelled;
        campaign.EndDate = DateTimeOffset.UtcNow;
        campaign.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<ProcessQueueResultDto>> ProcessQueueAsync(int campaignId, int batchSize = 50, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns.Include(c => c.MessageTemplate).FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        if (campaign is null)
            return Result<ProcessQueueResultDto>.Failure("Campaña no encontrada.");

        if (campaign.Status != CampaignStatus.Running)
            return Result<ProcessQueueResultDto>.Failure($"La campaña está en estado {campaign.Status}, no se puede procesar.");

        if (await IsKillSwitchEnabledAsync(ct))
            return Result<ProcessQueueResultDto>.Failure("El Kill Switch está activo: no se pueden enviar mensajes.");

        var contactChannel = MapChannel(campaign.Channel);

        var recipients = await db.CampaignRecipients
            .Include(r => r.Prospect).ThenInclude(p => p.Contacts)
            .Where(r => r.CampaignId == campaignId &&
                        (r.Status == CampaignRecipientStatus.Pending || r.Status == CampaignRecipientStatus.Queued))
            .Take(batchSize)
            .ToListAsync(ct);

        var sent = 0;
        var failed = 0;
        var suppressed = 0;

        foreach (var recipient in recipients)
        {
            recipient.Attempts++;
            recipient.LastAttemptAt = DateTimeOffset.UtcNow;
            recipient.UpdatedAt = DateTimeOffset.UtcNow;

            var contact = contactChannel is null
                ? null
                : recipient.Prospect.Contacts.FirstOrDefault(c => c.Channel == contactChannel && c.IsActive);

            if (contact is null)
            {
                recipient.Status = CampaignRecipientStatus.Skipped;
                continue;
            }

            if (await IsContactSuppressedAsync(campaign.Channel, contact.Value, ct))
            {
                recipient.Status = CampaignRecipientStatus.Stopped;
                suppressed++;
                continue;
            }

            var content = TemplateRenderer.Render(campaign.MessageTemplate.Content, recipient.Prospect);
            var sendResult = await messageProvider.SendAsync(
                new SendMessageRequest(campaign.Channel, contact.Value, content, recipient.Prospect.BusinessName), ct);

            var message = new Message
            {
                OrganizationId = campaign.OrganizationId,
                ProspectId = recipient.ProspectId,
                CampaignId = campaign.Id,
                CampaignRecipientId = recipient.Id,
                TemplateId = campaign.MessageTemplateId,
                Channel = campaign.Channel,
                Provider = messageProvider.ProviderName,
                // Igual que en TestMessageService: con plantilla de Meta configurada, lo que se
                // manda de verdad es el copy aprobado por Meta, no campaign.MessageTemplate.Content
                // (que es solo la referencia local). sendResult.SentContent trae ese texto real
                // cuando el proveedor pudo resolverlo.
                Content = sendResult.SentContent ?? content,
                ExternalMessageId = sendResult.ExternalMessageId,
                Status = sendResult.Success ? MessageStatus.Sent : MessageStatus.Failed,
                SentAt = sendResult.Success ? DateTimeOffset.UtcNow : null,
                FailedAt = sendResult.Success ? null : DateTimeOffset.UtcNow
            };
            db.Messages.Add(message);

            recipient.FirstMessageId ??= message.Id;
            recipient.LastMessageId = message.Id;
            recipient.Status = sendResult.Success ? CampaignRecipientStatus.Sent : CampaignRecipientStatus.Failed;

            if (sendResult.Success)
            {
                sent++;
                recipient.Prospect.LastContactedAt = DateTimeOffset.UtcNow;
                if (recipient.Prospect.Status is ProspectStatus.New or ProspectStatus.Validated or ProspectStatus.Ready)
                    recipient.Prospect.Status = ProspectStatus.Contacted;
            }
            else
            {
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);

        return Result<ProcessQueueResultDto>.Success(new ProcessQueueResultDto(recipients.Count, sent, failed, suppressed));
    }

    public async Task<PagedResult<CampaignRecipientDto>> SearchRecipientsAsync(
        int? campaignId, CampaignRecipientStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var recipients = db.CampaignRecipients.AsQueryable();
        if (campaignId is not null)
            recipients = recipients.Where(r => r.CampaignId == campaignId);
        if (status is not null)
            recipients = recipients.Where(r => r.Status == status);

        var recipientDtos = await recipients
            .Select(r => new CampaignRecipientDto(
                r.Id,
                r.CampaignId,
                r.Campaign.Name,
                r.ProspectId,
                r.Prospect.BusinessName,
                r.Status,
                r.Attempts,
                r.LastAttemptAt,
                db.Messages.Where(m => m.Id == r.LastMessageId).Select(m => m.FailureReason).FirstOrDefault(),
                db.Messages.Where(m => m.Id == r.LastMessageId).Select(m => m.FailedAt).FirstOrDefault(),
                r.CreatedAt,
                true))
            .ToListAsync(ct);

        // Envíos individuales de prueba (TestMessageService) mandan un Message suelto sin pasar
        // por Campaign/CampaignRecipient. Sin esto, un envío fallido/pendiente hecho a mano nunca
        // aparece acá sin importar qué estado se elija en el filtro. Queued/Skipped/Stopped no
        // tienen equivalente en MessageStatus, así que solo se suman para Failed/Pending (o "todos").
        // Se combinan en memoria (en vez de Concat a nivel SQL) porque EF no puede traducir un set
        // operation sobre queries ya proyectadas a este DTO.
        var adHocDtos = new List<CampaignRecipientDto>();
        if (campaignId is null && status is null or CampaignRecipientStatus.Failed or CampaignRecipientStatus.Pending)
        {
            var adHocMessages = db.Messages.Where(m => m.CampaignId == null);
            adHocMessages = status switch
            {
                CampaignRecipientStatus.Failed => adHocMessages.Where(m => m.Status == MessageStatus.Failed),
                CampaignRecipientStatus.Pending => adHocMessages.Where(m => m.Status == MessageStatus.Pending),
                _ => adHocMessages.Where(m => m.Status == MessageStatus.Failed || m.Status == MessageStatus.Pending)
            };

            adHocDtos = await adHocMessages
                .Select(m => new CampaignRecipientDto(
                    m.Id,
                    null,
                    null,
                    m.ProspectId,
                    m.Prospect.BusinessName,
                    m.Status == MessageStatus.Failed ? CampaignRecipientStatus.Failed : CampaignRecipientStatus.Pending,
                    1,
                    m.SentAt ?? m.FailedAt ?? m.CreatedAt,
                    m.FailureReason,
                    m.FailedAt,
                    m.CreatedAt,
                    false))
                .ToListAsync(ct);
        }

        var combined = recipientDtos.Concat(adHocDtos)
            .OrderByDescending(r => r.LastAttemptAt ?? r.CreatedAt)
            .ToList();

        var items = combined.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<CampaignRecipientDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = combined.Count };
    }

    // Solo reintenta destinatarios en estado Failed (un envío que se intentó y falló), de
    // campañas que todavía admiten envíos. Los Skipped (sin contacto válido) y Stopped
    // (suprimidos/opt-out) quedan afuera a propósito: Skipped se resuelve cargando el contacto,
    // no con un reintento; Stopped no debe reintentarse nunca, sería pisar una baja/suppression.
    // No pisa el resto de recipient.Attempts/LastAttemptAt acá: eso lo actualiza ProcessQueueAsync
    // en el próximo intento real de envío, este método solo vuelve a poner al recipient en cola.
    public async Task<Result<RetryRecipientsResultDto>> RetryRecipientsAsync(RetryRecipientsRequest request, CancellationToken ct = default)
    {
        var recipients = await db.CampaignRecipients
            .Include(r => r.Campaign)
            .Where(r => request.RecipientIds.Contains(r.Id))
            .ToListAsync(ct);

        var retried = 0;
        foreach (var recipient in recipients)
        {
            if (recipient.Status != CampaignRecipientStatus.Failed)
                continue;
            if (recipient.Campaign.Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
                continue;

            recipient.Status = CampaignRecipientStatus.Pending;
            recipient.UpdatedAt = DateTimeOffset.UtcNow;
            retried++;
        }

        var skipped = request.RecipientIds.Count - retried;

        await db.SaveChangesAsync(ct);

        return Result<RetryRecipientsResultDto>.Success(new RetryRecipientsResultDto(retried, skipped));
    }

    // Solo saca al destinatario de la campaña (ej. para limpiar la lista de "No enviados"); no
    // toca los Message que ya se hayan generado para él, igual que Borrar en la pestaña
    // "Enviados" no afecta el historial de Costos (son registros independientes a propósito).
    public async Task<Result<bool>> DeleteRecipientAsync(int id, CancellationToken ct = default)
    {
        var recipient = await db.CampaignRecipients.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (recipient is null)
            return Result<bool>.Failure("Destinatario no encontrado.");

        db.CampaignRecipients.Remove(recipient);
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<int>> DeleteRecipientsAsync(IReadOnlyCollection<int> recipientIds, CancellationToken ct = default)
    {
        if (recipientIds.Count == 0)
            return Result<int>.Success(0);

        var recipients = await db.CampaignRecipients.Where(r => recipientIds.Contains(r.Id)).ToListAsync(ct);
        db.CampaignRecipients.RemoveRange(recipients);
        await db.SaveChangesAsync(ct);

        return Result<int>.Success(recipients.Count);
    }

    public async Task SetKillSwitchAsync(KillSwitchRequest request, CancellationToken ct = default)
    {
        var organizationId = currentUser.OrganizationId!.Value;
        var setting = await db.OrganizationSettings
            .FirstOrDefaultAsync(s => s.OrganizationId == organizationId && s.Key == KillSwitchKey, ct);

        if (setting is null)
        {
            db.OrganizationSettings.Add(new OrganizationSettings
            {
                OrganizationId = organizationId,
                Key = KillSwitchKey,
                Value = request.Enabled.ToString()
            });
        }
        else
        {
            setting.Value = request.Enabled.ToString();
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsKillSwitchEnabledAsync(CancellationToken ct = default)
    {
        var organizationId = currentUser.OrganizationId!.Value;
        var value = await db.OrganizationSettings
            .Where(s => s.OrganizationId == organizationId && s.Key == KillSwitchKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return bool.TryParse(value, out var enabled) && enabled;
    }

    private async Task<Campaign?> GetEditableCampaignAsync(int campaignId, CancellationToken ct)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct);
        return campaign is not null && campaign.Status is CampaignStatus.Draft or CampaignStatus.Ready or CampaignStatus.Paused
            ? campaign
            : null;
    }

    private async Task<AddRecipientsResultDto> AddRecipientsInternalAsync(Campaign campaign, IReadOnlyCollection<int> prospectIds, CancellationToken ct)
    {
        var added = 0;
        var alreadyInCampaign = 0;
        var withoutValidContact = 0;
        var suppressedCount = 0;

        var contactChannel = MapChannel(campaign.Channel);

        var existingProspectIds = await db.CampaignRecipients
            .Where(r => r.CampaignId == campaign.Id)
            .Select(r => r.ProspectId)
            .ToListAsync(ct);
        var existingSet = existingProspectIds.ToHashSet();

        foreach (var prospectId in prospectIds)
        {
            if (!existingSet.Add(prospectId))
            {
                alreadyInCampaign++;
                continue;
            }

            var contact = contactChannel is null
                ? null
                : await db.ProspectContacts
                    .Where(c => c.ProspectId == prospectId && c.Channel == contactChannel && c.IsActive)
                    .OrderByDescending(c => c.IsPrimary)
                    .FirstOrDefaultAsync(ct);

            if (contact is null)
            {
                withoutValidContact++;
                continue;
            }

            if (await IsContactSuppressedAsync(campaign.Channel, contact.Value, ct))
            {
                suppressedCount++;
                continue;
            }

            db.CampaignRecipients.Add(new CampaignRecipient
            {
                OrganizationId = campaign.OrganizationId,
                CampaignId = campaign.Id,
                ProspectId = prospectId,
                Status = CampaignRecipientStatus.Pending
            });
            added++;
        }

        await db.SaveChangesAsync(ct);

        return new AddRecipientsResultDto(added, alreadyInCampaign, withoutValidContact, suppressedCount);
    }

    private async Task<bool> IsContactSuppressedAsync(MessagingChannel channel, string contactValue, CancellationToken ct)
    {
        var suppressionType = channel switch
        {
            MessagingChannel.Whatsapp => SuppressionContactType.Whatsapp,
            MessagingChannel.Email => SuppressionContactType.Email,
            _ => (SuppressionContactType?)null
        };

        return suppressionType is not null && await suppressionService.IsSuppressedAsync(suppressionType.Value, contactValue, ct);
    }

    private static ProspectContactChannel? MapChannel(MessagingChannel channel) => channel switch
    {
        MessagingChannel.Whatsapp => ProspectContactChannel.Whatsapp,
        MessagingChannel.Email => ProspectContactChannel.Email,
        _ => null
    };

    private async Task<CampaignDto?> LoadDtoAsync(int id, CancellationToken ct)
    {
        return await db.Campaigns
            .Where(c => c.Id == id)
            .Select(c => new CampaignDto(
                c.Id,
                c.Name,
                c.Description,
                c.Status,
                c.Channel,
                c.MessageTemplateId,
                c.MessageTemplate.Name,
                c.MaxMessages,
                c.MessagesPerMinute,
                c.MessagesPerHour,
                c.MessagesPerDay,
                c.StartDate,
                c.EndDate,
                c.Recipients.Count,
                c.Recipients.Count(r => r.Status == CampaignRecipientStatus.Sent || r.Status == CampaignRecipientStatus.Delivered ||
                                         r.Status == CampaignRecipientStatus.Responded || r.Status == CampaignRecipientStatus.Interested ||
                                         r.Status == CampaignRecipientStatus.NotInterested),
                c.Recipients.Count(r => r.Status == CampaignRecipientStatus.Responded || r.Status == CampaignRecipientStatus.Interested ||
                                         r.Status == CampaignRecipientStatus.NotInterested),
                c.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
