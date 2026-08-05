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
    ITelegramNotifier telegramNotifier,
    ILogger<InboundMessageService> logger) : IInboundMessageService
{
    private const decimal ConfidenceThreshold = 0.80m;

    // Modelo/versión sintéticos para clasificaciones que no pasaron por el clasificador de
    // reglas: el tap de un botón es una señal de compra más fuerte y determinista que
    // cualquier frase, así que se registra aparte para poder distinguirla en auditoría.
    private const string QuickReplyModelName = "quick-reply-button-v1";
    private const string QuickReplyPromptVersion = "n/a";

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

        var prospectContact = await FindProspectContactAsync(request.OrganizationId, normalizedContact, ct);

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

        // Solo se interpreta como tap de botón cuando ButtonPayload no es nulo. Nunca se corre
        // el fallback por texto sobre contenido de texto libre arbitrario: eso podría marcar
        // como Interested a un prospecto que simplemente mencione "interesado" de pasada.
        var isInterestButtonTap = request.ButtonPayload is not null
            && QuickReplyButtonMapper.IsInterestTap(request.ButtonPayload, request.Content);

        // Tocar el botón "Estoy interesado" es una señal de compra más fuerte y determinista
        // que cualquier frase del clasificador de reglas: se sintetiza Interested con confianza
        // máxima en vez de pasar por IIntentClassifier. No toca Prospect.Category: con un solo
        // botón genérico ya no hay rubro que autodeclarar acá, el ruteo Administración/Ventas
        // sigue leyendo Category tal cual esté cargado de antes (import, carga manual, etc.).
        var classification = isInterestButtonTap
            ? new IntentClassificationResult(IntentClassification.Interested, 1.00m, QuickReplyModelName, QuickReplyPromptVersion)
            : await intentClassifier.ClassifyAsync(request.Content, ct);

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
            ButtonPayload = request.ButtonPayload,
            ProcessedAt = DateTimeOffset.UtcNow
        };
        db.MessageResponses.Add(messageResponse);

        var recipient = campaignRecipientId is null
            ? null
            : await db.CampaignRecipients.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == campaignRecipientId, ct);

        Lead? lead = null;
        var leadCreated = false;
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

                (lead, leadCreated) = await CreateOrReuseLeadAsync(request.OrganizationId, prospect, campaignId, effective, ct);
                await SendCatalogIfConfiguredAsync(request.OrganizationId, prospect, normalizedContact, ct);
                break;

            case IntentClassification.Question:
                if (prospect.Status is not (ProspectStatus.Lead or ProspectStatus.Customer))
                    prospect.Status = ProspectStatus.Lead;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Interested;

                (lead, leadCreated) = await CreateOrReuseLeadAsync(request.OrganizationId, prospect, campaignId, effective, ct);
                break;

            default:
                prospect.Status = ProspectStatus.Responded;
                if (recipient is not null) recipient.Status = CampaignRecipientStatus.Responded;
                break;
        }

        await db.SaveChangesAsync(ct);

        // La notificación al vendedor corre después de guardar: un envío lento o fallido de
        // WhatsApp nunca debe demorar ni revertir la persistencia del lead. Solo se notifica en
        // un lead genuinamente nuevo, no en cada mensaje de seguimiento de un lead ya abierto.
        if (lead is not null && leadCreated)
            await NotifyAssigneeAsync(lead, prospect, request.Content, ct);

        return Result<InboundMessageResultDto>.Success(
            new InboundMessageResultDto(messageResponse.Id, classification.Classification, classification.Confidence, lead?.Id, suppressed));
    }

    // El inbound real de Meta siempre trae el "9" móvil argentino en "from" (ver
    // ArgentineMobileDetector), pero ContactValueNormalizer no lo inserta al guardar un contacto
    // cargado a mano o importado, así que un mismo número puede estar guardado con o sin él. Si
    // el match exacto falla, se reintenta contra la variante sin el "9" antes de dar por perdido
    // el prospecto, sin cambiar cómo se normaliza/guarda en el resto del sistema.
    private async Task<ProspectContact?> FindProspectContactAsync(int organizationId, string normalizedContact, CancellationToken ct)
    {
        var contact = await db.ProspectContacts.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == organizationId && c.Value == normalizedContact)
            .FirstOrDefaultAsync(ct);
        if (contact is not null)
            return contact;

        var withoutMobilePrefix = ArgentineMobileDetector.WithoutMobilePrefix(normalizedContact);
        if (withoutMobilePrefix is null)
            return null;

        return await db.ProspectContacts.IgnoreQueryFilters()
            .Where(c => c.OrganizationId == organizationId && c.Value == withoutMobilePrefix)
            .FirstOrDefaultAsync(ct);
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

        // Estamos dentro de la ventana de servicio de 24hs (el prospecto acaba de escribir),
        // así que texto libre es legal y hay que forzarlo: si no, con TemplateName configurado
        // esto reenvía la plantilla de bienvenida en vez del catálogo real.
        var sendResult = await messageProvider.SendAsync(
            new SendMessageRequest(MessagingChannel.Whatsapp, contact, content, PreferFreeText: true), ct);

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

    private async Task<(Lead Lead, bool Created)> CreateOrReuseLeadAsync(
        int organizationId, Prospect prospect, int? campaignId, IntentClassification classification, CancellationToken ct)
    {
        var openLead = await db.Leads.IgnoreQueryFilters()
            .Where(l => l.OrganizationId == organizationId && l.ProspectId == prospect.Id &&
                        (l.Status == LeadStatus.New || l.Status == LeadStatus.InProgress))
            .FirstOrDefaultAsync(ct);

        if (openLead is not null)
        {
            openLead.LastActivityAt = DateTimeOffset.UtcNow;
            return (openLead, false);
        }

        var area = LeadRouting.AreaFor(prospect.Category);
        var assignee = await LeadAssignment.PickNextAssigneeAsync(db, organizationId, area, ct);
        if (assignee is null)
        {
            // Nunca dejar un lead sin asignar en silencio: si el área todavía no tiene
            // usuarios, se cae al round-robin general y queda logueado para que se note.
            assignee = await LeadAssignment.PickNextAssigneeAsync(db, organizationId, ct);
            logger.LogWarning(
                "[LeadAssignment] Organización {OrganizationId}: no hay usuarios activos en el área {Area}. " +
                "El lead del prospecto {ProspectId} se asignó por round-robin general al usuario {UserId}.",
                organizationId, area, prospect.Id, assignee);
        }

        var lead = new Lead
        {
            OrganizationId = organizationId,
            ProspectId = prospect.Id,
            CampaignId = campaignId,
            AssignedToUserId = assignee,
            Status = LeadStatus.New,
            Priority = classification == IntentClassification.Question ? LeadPriority.High : LeadPriority.Medium,
            FirstResponseAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        db.Leads.Add(lead);
        return (lead, true);
    }

    // Avisa al vendedor/administrativo asignado, por WhatsApp y por Telegram como canales
    // independientes (cada uno solo si el usuario tiene el dato cargado). Nunca debe romper el
    // procesamiento del webhook: un fallo acá (de red, de configuración, lo que sea) solo se
    // loguea, porque un 500 en el webhook hace que Meta reintente el mismo evento para siempre.
    private async Task NotifyAssigneeAsync(Lead lead, Prospect prospect, string prospectMessage, CancellationToken ct)
    {
        try
        {
            if (lead.AssignedToUserId is null)
                return;

            var assignee = await db.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == lead.AssignedToUserId.Value, ct);

            if (assignee is null)
                return;

            var freeText = LeadHandoffMessageBuilder.BuildFreeText(prospect, prospectMessage);

            await NotifyByWhatsAppAsync(lead, assignee.Phone, prospect, prospectMessage, freeText, ct);
            await NotifyByTelegramAsync(lead, assignee.TelegramChatId, freeText, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[LeadHandoff] Error inesperado notificando el lead {LeadId}.", lead.Id);
        }
    }

    private async Task NotifyByWhatsAppAsync(
        Lead lead, string? assigneePhone, Prospect prospect, string prospectMessage, string freeText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assigneePhone))
        {
            logger.LogWarning(
                "[LeadHandoff] Usuario {UserId} asignado al lead {LeadId} no tiene teléfono cargado, no se pudo notificar por WhatsApp.",
                lead.AssignedToUserId, lead.Id);
            return;
        }

        var contact = ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, assigneePhone);

        // Preferir la plantilla UTILITY de handoff cuando está configurada: el usuario interno
        // nunca le escribió al número del negocio, así que texto libre le va a fallar con error
        // 131047 fuera de la ventana de servicio de 24hs. Sin plantilla configurada, se intenta
        // texto libre igual (sirve en dev, no en producción).
        var handoffTemplateName = messageProvider.HandoffTemplateName;
        var sendRequest = string.IsNullOrWhiteSpace(handoffTemplateName)
            ? new SendMessageRequest(MessagingChannel.Whatsapp, contact, freeText, PreferFreeText: true)
            : new SendMessageRequest(
                MessagingChannel.Whatsapp, contact, freeText,
                TemplateNameOverride: handoffTemplateName,
                TemplateParameters: LeadHandoffMessageBuilder.BuildTemplateParameters(prospect, prospectMessage));

        var sendResult = await messageProvider.SendAsync(sendRequest, ct);

        if (!sendResult.Success)
        {
            logger.LogWarning(
                "[LeadHandoff] No se pudo notificar por WhatsApp al usuario {UserId} sobre el lead {LeadId}: {Error}",
                lead.AssignedToUserId, lead.Id, sendResult.Error);
        }
    }

    // A diferencia de WhatsApp, Telegram no tiene ventana de servicio de 24hs ni requiere
    // plantilla aprobada: siempre es texto libre.
    private async Task NotifyByTelegramAsync(Lead lead, string? assigneeTelegramChatId, string freeText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assigneeTelegramChatId))
            return;

        var telegramResult = await telegramNotifier.SendAsync(assigneeTelegramChatId, freeText, ct);

        if (!telegramResult.Success)
        {
            logger.LogWarning(
                "[LeadHandoff] No se pudo notificar por Telegram al usuario {UserId} sobre el lead {LeadId}: {Error}",
                lead.AssignedToUserId, lead.Id, telegramResult.Error);
        }
    }
}
