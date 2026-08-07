using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record MessageTemplateDto(int Id, string Name, string Content, MessagingChannel Channel, int Version, bool IsActive, bool IsCatalogTemplate);

public record CreateMessageTemplateRequest(string Name, string Content, MessagingChannel Channel);

public record UpdateMessageTemplateRequest(string Name, string Content);

// Reflejo de una plantilla tal como está aprobada en Meta Business Manager (Graph API) — no
// existe en nuestra base hasta que se sincroniza con SyncMessageTemplateFromMetaRequest.
public record MetaWhatsAppTemplateDto(string Name, string Language, string Status, string? BodyText);

public record SyncMessageTemplateFromMetaRequest(string Name, string Language);
