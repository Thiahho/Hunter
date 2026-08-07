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

// Dispara los mensajes programados desde la ficha de un prospecto ("Programar mensaje") — ver
// ScheduledMessageService.RunAsync, que hace el envío real. Mismo patrón que
// ScheduledProspectAutomationBackgroundService: un solo mensaje es rápido (una llamada HTTP), así
// que no hace falta el Task.Run "fire-and-forget" que usa esa otra automatización para no bloquear
// el polling — se procesan secuencialmente dentro del mismo tick.
public class ScheduledMessageBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledMessageOptions> options,
    ILogger<ScheduledMessageBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, options.Value.PollIntervalSeconds));
        logger.LogInformation("[ScheduledMessage] Iniciado, revisando mensajes programados vencidos cada {Interval}.", interval);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await DispatchDueMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "[ScheduledMessage] Error revisando mensajes programados vencidos.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task DispatchDueMessagesAsync(CancellationToken ct)
    {
        using var scanScope = scopeFactory.CreateScope();
        var db = scanScope.ServiceProvider.GetRequiredService<IHunterDbContext>();

        var now = DateTimeOffset.UtcNow;
        var due = await db.ScheduledMessages
            .IgnoreQueryFilters()
            .Where(s => s.Status == ScheduledMessageStatus.Pending && s.ScheduledAt <= now)
            .Select(s => new { s.Id, s.OrganizationId })
            .ToListAsync(ct);

        foreach (var message in due)
        {
            logger.LogInformation("[ScheduledMessage] Disparando mensaje programado {Id} (org {OrganizationId}).", message.Id, message.OrganizationId);

            try
            {
                using var orgScope = CurrentUserService.UseOrganization(message.OrganizationId);
                using var workScope = scopeFactory.CreateScope();
                var scheduledMessageService = workScope.ServiceProvider.GetRequiredService<IScheduledMessageService>();

                await scheduledMessageService.RunAsync(message.Id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // ScheduledMessageService.RunAsync ya captura sus propios fallos de negocio en
                // FailureReason; esto solo cubre algo verdaderamente inesperado (ej. el scope se
                // rompe) para que un mensaje no tumbe el resto del tick.
                logger.LogError(ex, "[ScheduledMessage] Fallo inesperado ejecutando el mensaje programado {Id}.", message.Id);
            }
        }
    }
}
