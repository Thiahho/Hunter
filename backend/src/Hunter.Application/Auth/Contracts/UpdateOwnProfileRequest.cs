namespace Hunter.Application.Auth.Contracts;

// Autoservicio, sin Role/Area/IsActive: esos los administra OWNER/ADMIN vía UsersController.
// TelegramChatId manual cubre el caso de no poder/querer pasar por el flujo /start del bot.
public record UpdateOwnProfileRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? TelegramChatId = null);
