using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Common;
using Hunter.Application.Compliance;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Compliance;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

// Envío manual de un único mensaje a UN prospecto puntual, sin pasar por el flujo de
// Campaign/CampaignRecipient. Pensado para pruebas en desarrollo: crear un prospecto de
// prueba con tu número personal y mandarle el mensaje de bienvenida real, sin arriesgar
// enviarle nada a los prospectos reales sacados de Google Places.
public class TestMessageService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    ISuppressionService suppressionService,
    IMessageProvider messageProvider) : ITestMessageService
{
    public async Task<Result<TestMessageResultDto>> SendAsync(int prospectId, SendTestMessageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return Result<TestMessageResultDto>.Failure("El contenido del mensaje no puede estar vacío.");

        var organizationId = currentUser.OrganizationId!.Value;

        if (await IsKillSwitchEnabledAsync(organizationId, ct))
            return Result<TestMessageResultDto>.Failure("El Kill Switch está activo: no se pueden enviar mensajes.");

        var prospect = await db.Prospects
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == prospectId, ct);
        if (prospect is null)
            return Result<TestMessageResultDto>.Failure("Prospecto no encontrado.");

        var contact = prospect.Contacts.FirstOrDefault(c => c.Channel == ProspectContactChannel.Whatsapp && c.IsActive);
        if (contact is null)
            return Result<TestMessageResultDto>.Failure("El prospecto no tiene un contacto de WhatsApp cargado.");

        if (await suppressionService.IsSuppressedAsync(SuppressionContactType.Whatsapp, contact.Value, ct))
            return Result<TestMessageResultDto>.Failure("Ese contacto está en la lista de exclusión (suppression) y no puede recibir mensajes.");

        var content = TemplateRenderer.Render(request.Content, prospect);
        var sendResult = await messageProvider.SendAsync(
            new SendMessageRequest(MessagingChannel.Whatsapp, contact.Value, content), ct);

        var message = new Message
        {
            OrganizationId = organizationId,
            ProspectId = prospect.Id,
            Channel = MessagingChannel.Whatsapp,
            Provider = messageProvider.ProviderName,
            Content = content,
            ExternalMessageId = sendResult.ExternalMessageId,
            Status = sendResult.Success ? MessageStatus.Sent : MessageStatus.Failed,
            SentAt = sendResult.Success ? DateTimeOffset.UtcNow : null,
            FailedAt = sendResult.Success ? null : DateTimeOffset.UtcNow
        };
        db.Messages.Add(message);

        if (sendResult.Success)
            prospect.LastContactedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return Result<TestMessageResultDto>.Success(
            new TestMessageResultDto(message.Id, sendResult.Success, sendResult.ExternalMessageId, sendResult.Error));
    }

    private async Task<bool> IsKillSwitchEnabledAsync(int organizationId, CancellationToken ct)
    {
        var value = await db.OrganizationSettings
            .Where(s => s.OrganizationId == organizationId && s.Key == OrganizationSettingsKeys.KillSwitch)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return bool.TryParse(value, out var enabled) && enabled;
    }
}
