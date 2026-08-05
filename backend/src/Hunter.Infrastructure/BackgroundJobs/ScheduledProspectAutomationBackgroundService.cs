using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Domain.Prospecting;
using Hunter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.BackgroundJobs;

// Dispara las automatizaciones programadas desde /app/prospects/search ("Programar
// automatización"): busca en OSM, importa todo lo válido y arranca/continúa el envío de la
// campaña — ver ScheduledProspectAutomationService.RunAsync, que hace el trabajo real. Cada
// automatización vencida corre en su propio Task.Run (no bloquea el loop de polling): RunAsync
// puede tardar minutos si tiene que vaciar la cola de envío respetando MessagesPerMinute.
public class ScheduledProspectAutomationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledProspectAutomationOptions> options,
    ILogger<ScheduledProspectAutomationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollIntervalSeconds));
        logger.LogInformation("[ProspectAutomation] Iniciado, revisando automatizaciones vencidas cada {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await DispatchDueAutomationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[ProspectAutomation] Error revisando automatizaciones vencidas.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task DispatchDueAutomationsAsync(CancellationToken ct)
    {
        using var scanScope = scopeFactory.CreateScope();
        var db = scanScope.ServiceProvider.GetRequiredService<IHunterDbContext>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.ScheduledProspectAutomations
            .IgnoreQueryFilters()
            .Where(a => a.Status == ScheduledAutomationStatus.Pending && a.ScheduledAt <= now)
            .Select(a => new { a.Id, a.OrganizationId })
            .ToListAsync(ct);

        foreach (var automation in due)
        {
            logger.LogInformation("[ProspectAutomation] Disparando automatización {Id} (org {OrganizationId}).", automation.Id, automation.OrganizationId);

            // Fire-and-forget deliberado: RunAsync puede tardar minutos (drena la cola de envío
            // con delays entre lotes) y no debe bloquear el próximo tick del polling ni las demás
            // automatizaciones vencidas en este mismo ciclo.
            _ = RunInBackgroundAsync(automation.Id, automation.OrganizationId, stoppingTokenForLifetime: ct);
        }
    }

    private async Task RunInBackgroundAsync(int automationId, int organizationId, CancellationToken stoppingTokenForLifetime)
    {
        try
        {
            using var orgScope = CurrentUserService.UseOrganization(organizationId);
            using var workScope = scopeFactory.CreateScope();
            var automationService = workScope.ServiceProvider.GetRequiredService<IScheduledProspectAutomationService>();

            await automationService.RunAsync(automationId, stoppingTokenForLifetime);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ScheduledProspectAutomationService.RunAsync ya captura sus propios fallos de
            // negocio en ResultSummary; esto solo cubre algo verdaderamente inesperado (ej. el
            // scope se rompe) para que nunca quede un Task sin observar tirando una excepción.
            logger.LogError(ex, "[ProspectAutomation] Fallo inesperado ejecutando la automatización {Id}.", automationId);
        }
    }
}
