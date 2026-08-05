using Hunter.Domain.Identity;

namespace Hunter.Application.Auth.Contracts;

// Único endpoint para setear Phone/Area en un usuario existente (no hay pantalla de usuarios
// en el frontend todavía). Todos los campos son opcionales: solo se actualiza lo que viene
// no-nulo, así que un PATCH parcial no pisa el resto.
public record UpdateUserRequest(
    string? Phone = null,
    string? TelegramChatId = null,
    UserArea? Area = null,
    bool? IsActive = null);
