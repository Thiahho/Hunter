using Hunter.Application.Common;
using Hunter.Domain.Prospecting;
using Hunter.Shared;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Crm;

public class ManualTelegramAlertService(
    IHunterDbContext db,
    ICurrentUserService currentUser,
    ITelegramNotifier telegramNotifier) : IManualTelegramAlertService
{
    public async Task<Result<bool>> SendAsync(int prospectId, CancellationToken ct = default)
    {
        var userId = currentUser.UserId;
        if (userId is null)
            return Result<bool>.Failure("No hay una sesión de usuario activa.");

        var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId.Value, ct);
        if (user is null)
            return Result<bool>.Failure("Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(user.TelegramChatId))
            return Result<bool>.Failure("Todavía no vinculaste tu Telegram. Vinculalo desde tu perfil para poder recibir alertas.");

        var prospect = await db.Prospects
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == prospectId, ct);
        if (prospect is null)
            return Result<bool>.Failure("Prospecto no encontrado.");

        var whatsappContact = prospect.Contacts.FirstOrDefault(c => c.Channel == ProspectContactChannel.Whatsapp && c.IsActive);
        if (whatsappContact is null)
            return Result<bool>.Failure("El prospecto no tiene un contacto de WhatsApp cargado.");

        var lastInbound = await db.MessageResponses.IgnoreQueryFilters()
            .Where(r => r.ProspectId == prospectId)
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefaultAsync(ct);
        var prospectMessage = lastInbound?.Content ?? "(sin mensajes registrados)";

        var freeText = LeadHandoffMessageBuilder.BuildFreeText(
            prospect, prospectMessage, whatsappContact.Value, user.FirstName, isManualAlert: true);
        var button = LeadHandoffMessageBuilder.BuildWhatsAppButton(prospect, whatsappContact.Value, user.FirstName);

        var result = await telegramNotifier.SendAsync(user.TelegramChatId, freeText, button, ct);
        if (!result.Success)
            return Result<bool>.Failure(result.Error ?? "No se pudo enviar la alerta a Telegram.");

        return Result<bool>.Success(true);
    }
}
