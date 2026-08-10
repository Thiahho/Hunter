using Hunter.Application.Campaigning.Contracts;
using Hunter.Domain.Campaigning;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IMessageQueryService
{
    Task<PagedResult<MessageDto>> SearchAsync(
        int? campaignId, int? prospectId, MessageStatus? status, int page, int pageSize, CancellationToken ct = default);

    // "Prospectos del día" = prospectos creados (importados/scrapeados) en la fecha dada (hoy si
    // no se especifica), con su estado de contacto agregado desde Message. No filtra por
    // OrganizationId explícito porque HunterDbContext ya aplica el query filter global.
    Task<PagedResult<DailyProspectDto>> SearchDailyAsync(
        DateOnly? date, string? province, string? city, bool? sent, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyCollection<FailedContactDto>> GetFailedContactsAsync(
        DateOnly? date, string? province, string? city, CancellationToken ct = default);

    // Message no tiene soft-delete (a diferencia de Prospect): es un log, no un dato de negocio
    // que haya que poder recuperar. El costo asociado (Message.Cost) es solo informativo del
    // propio mensaje; el historial real de gastos vive aparte en Cost (ver ICostService), sin
    // relación (FK) con Message, así que borrar acá nunca afecta esos reportes.
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);
}
