using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Domain.Campaigning;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

public class ScheduledMessageService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    ITestMessageService testMessageService) : IScheduledMessageService
{
    public async Task<Result<ScheduledMessageDto>> CreateAsync(int prospectId, ScheduleMessageRequest request, CancellationToken ct = default)
    {
        if (request.ScheduledAt <= DateTimeOffset.UtcNow)
            return Result<ScheduledMessageDto>.Failure("La fecha y hora programada debe ser en el futuro.");

        var prospectExists = await db.Prospects.AnyAsync(p => p.Id == prospectId, ct);
        if (!prospectExists)
            return Result<ScheduledMessageDto>.Failure("Prospecto no encontrado.");

        var template = await db.MessageTemplates.FirstOrDefaultAsync(t => t.Id == request.MessageTemplateId, ct);
        if (template is null)
            return Result<ScheduledMessageDto>.Failure("La plantilla indicada no existe.");
        if (!template.IsActive)
            return Result<ScheduledMessageDto>.Failure("La plantilla indicada no está activa.");
        // Mismo límite que "Mensaje de prueba" (ITestMessageService.SendAsync): solo WhatsApp,
        // ver TestMessageService — no hay lookup de contacto genérico por canal todavía.
        if (template.Channel != MessagingChannel.Whatsapp)
            return Result<ScheduledMessageDto>.Failure("Por ahora solo se pueden programar mensajes de WhatsApp.");

        var scheduled = new ScheduledMessage
        {
            OrganizationId = currentUser.OrganizationId!.Value,
            CreatedByUserId = currentUser.UserId!.Value,
            ProspectId = prospectId,
            MessageTemplateId = request.MessageTemplateId,
            ScheduledAt = request.ScheduledAt,
            Status = ScheduledMessageStatus.Pending
        };

        db.ScheduledMessages.Add(scheduled);
        await db.SaveChangesAsync(ct);

        return Result<ScheduledMessageDto>.Success(await ToDtoAsync(scheduled, ct));
    }

    public async Task<IReadOnlyCollection<ScheduledMessageDto>> ListByProspectAsync(int prospectId, CancellationToken ct = default)
    {
        var scheduled = await db.ScheduledMessages
            .Where(s => s.ProspectId == prospectId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync(ct);

        var dtos = new List<ScheduledMessageDto>(scheduled.Count);
        foreach (var item in scheduled)
            dtos.Add(await ToDtoAsync(item, ct));

        return dtos;
    }

    public async Task<Result<bool>> CancelAsync(int id, CancellationToken ct = default)
    {
        var scheduled = await db.ScheduledMessages.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scheduled is null)
            return Result<bool>.Failure("Mensaje programado no encontrado.");

        if (scheduled.Status != ScheduledMessageStatus.Pending)
            return Result<bool>.Failure($"El mensaje programado está en estado {scheduled.Status}, no se puede cancelar.");

        scheduled.Status = ScheduledMessageStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task RunAsync(int id, CancellationToken ct = default)
    {
        var scheduled = await db.ScheduledMessages
            .Include(s => s.MessageTemplate)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
        if (scheduled is null || scheduled.Status != ScheduledMessageStatus.Pending)
            return;

        scheduled.Status = ScheduledMessageStatus.Running;
        scheduled.RunAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var sendResult = await testMessageService.SendAsync(
                scheduled.ProspectId, new SendTestMessageRequest(scheduled.MessageTemplate.Content), ct);

            if (!sendResult.Succeeded)
            {
                scheduled.Status = ScheduledMessageStatus.Failed;
                scheduled.FailureReason = sendResult.Error;
            }
            else
            {
                scheduled.MessageId = sendResult.Value!.MessageId;
                scheduled.Status = sendResult.Value.Success ? ScheduledMessageStatus.Sent : ScheduledMessageStatus.Failed;
                scheduled.FailureReason = sendResult.Value.Error;
            }
        }
        catch (Exception ex)
        {
            scheduled.Status = ScheduledMessageStatus.Failed;
            scheduled.FailureReason = $"Error inesperado: {ex.Message}";
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<ScheduledMessageDto> ToDtoAsync(ScheduledMessage scheduled, CancellationToken ct)
    {
        var templateName = await db.MessageTemplates
            .Where(t => t.Id == scheduled.MessageTemplateId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "(plantilla eliminada)";

        return new ScheduledMessageDto(
            scheduled.Id,
            scheduled.ProspectId,
            scheduled.MessageTemplateId,
            templateName,
            scheduled.ScheduledAt,
            scheduled.Status,
            scheduled.RunAt,
            scheduled.MessageId,
            scheduled.FailureReason,
            scheduled.Source,
            scheduled.CreatedAt);
    }
}
