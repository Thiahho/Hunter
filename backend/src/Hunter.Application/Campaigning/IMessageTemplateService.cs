using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageTemplateService
{
    Task<IReadOnlyCollection<MessageTemplateDto>> ListAsync(CancellationToken ct = default);
    Task<Result<MessageTemplateDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<MessageTemplateDto>> CreateAsync(CreateMessageTemplateRequest request, CancellationToken ct = default);
    Task<Result<MessageTemplateDto>> UpdateAsync(int id, UpdateMessageTemplateRequest request, CancellationToken ct = default);
    Task<Result<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);

    // Marca esta plantilla como EL catálogo de la organización (desmarca cualquier otra).
    // InboundMessageService la usa para responder automáticamente cuando detecta INTERESTED.
    Task<Result<bool>> SetCatalogAsync(int id, CancellationToken ct = default);

    // Plantillas aprobadas en Meta Business Manager para la WABA configurada, listas para elegir
    // sin tipear contenido a mano.
    Task<Result<IReadOnlyList<MetaWhatsAppTemplateDto>>> ListMetaTemplatesAsync(CancellationToken ct = default);

    // Crea (o reactiva con una nueva versión) la plantilla local de WhatsApp a partir de una
    // aprobada en Meta, y desactiva cualquier otra plantilla de WhatsApp activa no-catálogo: solo
    // puede haber una a la vez (ver ScheduledProspectAutomationService.ResolveDefaultCampaignAsync).
    Task<Result<MessageTemplateDto>> SyncFromMetaAsync(SyncMessageTemplateFromMetaRequest request, CancellationToken ct = default);
}
