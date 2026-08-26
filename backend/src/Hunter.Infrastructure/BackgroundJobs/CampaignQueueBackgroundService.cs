using Hunter.Application.Campaigning;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.BackgroundJobs;

// Dispara automáticamente lo que antes solo se hacía a mano con
// POST /api/v1/campaigns/{id}/process-queue. Corre fuera de cualquier request HTTP, así que no
// hay claims de organización disponibles: por eso usa IgnoreQueryFilters para encontrar las
// campañas Running de TODAS las organizaciones, y CurrentUserService.UseOrganization para que,
// al procesar cada una, el resto del pipeline (ICampaignService, ISuppressionService, el filtro
// multi-tenant de HunterDbContext) funcione exactamente igual que si un usuario de esa
// organización hubiera llamado al endpoint manual.
public class CampaignQueueBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CampaignQueueOptions> options,
    ILogger<CampaignQueueBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (!opts.Enabled)
        {
            logger.LogInformation("[CampaignQueue] Deshabilitado (CampaignQueue:Enabled=false). El envío de campañas sigue siendo manual.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(5, opts.PollIntervalSeconds));
        logger.LogInformation("[CampaignQueue] Iniciado, revisando campañas Running cada {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await ProcessRunningCampaignsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un tick fallido (ej. la base momentáneamente caída) nunca debe tirar abajo el
                // servicio entero: se loguea y se reintenta en el siguiente ciclo.
                logger.LogError(ex, "[CampaignQueue] Error procesando el ciclo de campañas.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task ProcessRunningCampaignsAsync(CancellationToken ct)
    {
        using var scanScope = scopeFactory.CreateScope();
        var db = scanScope.ServiceProvider.GetRequiredService<IHunterDbContext>();

        var runningCampaigns = await db.Campaigns
            .IgnoreQueryFilters()
            .Where(c => c.Status == CampaignStatus.Running)
            .Select(c => new { c.Id, c.OrganizationId, c.MessagesPerMinute, c.Name })
            .ToListAsync(ct);

        foreach (var campaign in runningCampaigns)
        {
            try
            {
                // batchSize = MessagesPerMinute de la propia campaña: como este ciclo corre una vez
                // por PollIntervalSeconds (60s por defecto), un lote por tick respeta el límite que
                // ya existía en el modelo (Campaign.MessagesPerMinute) pero que nadie hacía cumplir.
                var batchSize = Math.Max(1, campaign.MessagesPerMinute);

                using var orgScope = CurrentUserService.UseOrganization(campaign.OrganizationId);
                using var workScope = scopeFactory.CreateScope();
                var campaignService = workScope.ServiceProvider.GetRequiredService<ICampaignService>();

                var result = await campaignService.ProcessQueueAsync(campaign.Id, batchSize, ct);

                if (!result.Succeeded)
                {
                    logger.LogWarning(
                        "[CampaignQueue] Campaña {CampaignId} ({Name}): no se pudo procesar: {Error}",
                        campaign.Id, campaign.Name, result.Error);
                }
                else if (result.Value!.Processed > 0)
                {
                    logger.LogInformation(
                        "[CampaignQueue] Campaña {CampaignId} ({Name}): {Sent} enviados, {Failed} fallidos, {Suppressed} suprimidos de {Processed} procesados.",
                        campaign.Id, campaign.Name, result.Value.Sent, result.Value.Failed, result.Value.Suppressed, result.Value.Processed);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un try/catch por campaña, no solo alrededor de todo el tick: sin esto, una
                // excepción no controlada procesando la campaña N dejaba sin procesar las
                // campañas N+1, N+2... de ese mismo ciclo hasta el próximo tick
                // (auditoria.md, hallazgo Medio #8).
                logger.LogError(ex,
                    "[CampaignQueue] Campaña {CampaignId} ({Name}): error inesperado procesándola, se sigue con el resto.",
                    campaign.Id, campaign.Name);
            }
        }
    }
}
