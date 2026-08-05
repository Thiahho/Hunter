using Hunter.Domain.Identity;

namespace Hunter.Application.Auth.Contracts;

// Todos los campos son opcionales: solo se actualiza lo que viene no-nulo, así que un PATCH
// parcial no pisa el resto. Email es el único campo restringido más allá del rol del endpoint
// (OWNER/ADMIN): UserService exige además que el caller sea específicamente OWNER, porque es el
// identificador de login y cambiarlo a la ligera puede pisar el acceso de otro usuario.
public record UpdateUserRequest(
    string? Email = null,
    string? Phone = null,
    string? TelegramChatId = null,
    UserArea? Area = null,
    bool? IsActive = null);
