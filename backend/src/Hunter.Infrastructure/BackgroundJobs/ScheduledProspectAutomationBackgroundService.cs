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
// automatización" y "Plan diario"): busca en OSM o Apify según Source, importa todo lo válido y
// lo suma a la campaña "de sistema" (sin enviar — ver ScheduledProspectAutomationService.RunAsync,
// que hace el trabajo real). Las vencidas de un mismo tick se procesan en serie, no en paralelo:
// con el plan diario puede haber varias venciendo junto, y correrlas todas a la vez multiplicaría
// las llamadas concurrentes contra Overpass/Apify sin necesidad — RunAsync ya no vacía ninguna
// cola de envío (eso se cortó), así que el costo de serializar es mínimo.
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

            // Secuencial a propósito (ver comentario de cabecera): RunAsync ya no vacía ninguna
            // cola de envío, así que esperar cada una antes de seguir con la próxima no bloquea el
            // polling por más que unos segundos por corrida.
            await RunOneAsync(automation.Id, automation.OrganizationId, ct);
        }
    }

    private async Task RunOneAsync(int automationId, int organizationId, CancellationToken ct)
    {
        try
        {
            using var orgScope = CurrentUserService.UseOrganization(organizationId);
            using var workScope = scopeFactory.CreateScope();
            var automationService = workScope.ServiceProvider.GetRequiredService<IScheduledProspectAutomationService>();

            await automationService.RunAsync(automationId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // ScheduledProspectAutomationService.RunAsync ya captura sus propios fallos de
            // negocio en ResultSummary; esto solo cubre algo verdaderamente inesperado (ej. el
            // scope se rompe) para que una automatización vencida no tumbe el resto del tick.
            logger.LogError(ex, "[ProspectAutomation] Fallo inesperado ejecutando la automatización {Id}.", automationId);
        }
    }
}
