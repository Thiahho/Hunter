using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record MessageTemplateDto(int Id, string Name, string Content, MessagingChannel Channel, int Version, bool IsActive, bool IsCatalogTemplate);

public record CreateMessageTemplateRequest(string Name, string Content, MessagingChannel Channel);

public record UpdateMessageTemplateRequest(string Name, string Content);
