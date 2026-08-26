using Hunter.Application.Crm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.BackgroundJobs;

// Recordatorio periódico para leads New/InProgress sin actividad reciente — ver
// StaleLeadEscalationService.EscalateStaleLeadsAsync, que hace el trabajo real. A diferencia de
// ScheduledMessageBackgroundService/ScheduledProspectAutomationBackgroundService no hay una fila
// por evento que dispare esto ni una organización puntual: cada tick barre todas las
// organizaciones de una sola vez.
public class StaleLeadEscalationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<StaleLeadEscalationOptions> options,
    ILogger<StaleLeadEscalationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(60, options.Value.PollIntervalSeconds));
        logger.LogInformation("[LeadEscalation] Iniciado, revisando leads estancados cada {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IStaleLeadEscalationService>();
                var escalated = await service.EscalateStaleLeadsAsync(stoppingToken);
                if (escalated > 0)
                    logger.LogInformation("[LeadEscalation] Se reenviaron {Count} recordatorios.", escalated);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[LeadEscalation] Error revisando leads estancados.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
