namespace Hunter.Application.Crm;

public interface IStaleLeadEscalationService
{
    // Barre TODAS las organizaciones en una sola pasada (a diferencia de ScheduledMessageService,
    // no hay una fila por evento que dispare esto: es un chequeo periódico global). Devuelve
    // cuántos leads se re-escalaron, solo para logging.
    Task<int> EscalateStaleLeadsAsync(CancellationToken ct = default);
}
