using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageQueryService
{
    Task<PagedResult<MessageDto>> SearchAsync(
        int? campaignId, int? prospectId, MessageStatus? status, int page, int pageSize, CancellationToken ct = default);

    // Message no tiene soft-delete (a diferencia de Prospect): es un log, no un dato de negocio
    // que haya que poder recuperar. El costo asociado (Message.Cost) es solo informativo del
    // propio mensaje; el historial real de gastos vive aparte en Cost (ver ICostService), sin
    // relación (FK) con Message, así que borrar acá nunca afecta esos reportes.
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
}
