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
}
