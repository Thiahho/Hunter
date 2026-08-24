using Hunter.Application.Common;
using Hunter.Application.Prospecting;
using Hunter.Infrastructure.GoogleDrive;
using Hunter.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hunter.Infrastructure.BackgroundJobs;

// Mantiene "Prospectos.xlsx" siempre actualizado en la carpeta de Drive configurada — ver
// ProspectDriveSyncService.SyncAsync, que hace el trabajo real (arma el Excel con
// ProspectExportService.ExportAllActiveAsync y lo sube/actualiza vía IGoogleDriveClient). Sin
// GoogleDriveOptions configurado (sin cuenta de servicio/carpeta) no hace falta un flag "Enabled"
// aparte: IsConfigured ya es el apagador — mismo criterio que Apify/GooglePlaces (se registra
// igual, pero no hace nada real hasta que haya credenciales).
public class ProspectDriveSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProspectDriveSyncOptions> options,
    IOptions<GoogleDriveOptions> googleDriveOptions,
    ILogger<ProspectDriveSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!googleDriveOptions.Value.IsConfigured)
        {
            logger.LogInformation(
                "[ProspectDriveSync] Sin configurar (GoogleDrive:ServiceAccountKeyBase64/FolderId) — no se sincroniza nada.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(5, options.Value.PollIntervalMinutes));
        logger.LogInformation("[ProspectDriveSync] Iniciado, sincronizando el Excel de prospectos cada {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await SyncAllOrganizationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[ProspectDriveSync] Error en el ciclo de sincronización.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task SyncAllOrganizationsAsync(CancellationToken ct)
    {
        using var scanScope = scopeFactory.CreateScope();
        var db = scanScope.ServiceProvider.GetRequiredService<IHunterDbContext>();

        var organizationIds = await db.Organizations.IgnoreQueryFilters().Select(o => o.Id).ToListAsync(ct);

        foreach (var organizationId in organizationIds)
        {
            try
            {
                using var orgScope = CurrentUserService.UseOrganization(organizationId);
                using var workScope = scopeFactory.CreateScope();
                var syncService = workScope.ServiceProvider.GetRequiredService<IProspectDriveSyncService>();

                var result = await syncService.SyncAsync(ct);
                if (!result.Succeeded)
                {
                    logger.LogWarning("[ProspectDriveSync] Org {OrganizationId}: no se pudo sincronizar: {Error}", organizationId, result.Error);
                }
                else
                {
                    logger.LogInformation(
                        "[ProspectDriveSync] Org {OrganizationId}: {Count} prospectos sincronizados en {Url}.",
                        organizationId, result.Value!.ProspectCount, result.Value.DriveUrl);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Un try/catch por organización, no solo alrededor de todo el tick: una falla
                // sincronizando la organización N no debe dejar sin sincronizar las siguientes
                // (mismo criterio que CampaignQueueBackgroundService).
                logger.LogError(ex, "[ProspectDriveSync] Org {OrganizationId}: error inesperado, se sigue con el resto.", organizationId);
            }
        }
    }
}
