namespace Hunter.Application.Auth.Contracts;

public record CurrentUserDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? TelegramChatId,
    int OrganizationId,
    IReadOnlyCollection<string> Roles,
    bool TelegramConnected);

public record TelegramLinkDto(string DeepLink, DateTimeOffset ExpiresAt);
