using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning.Contracts;

public record InboundMessageRequest(
    int OrganizationId,
    string Contact,
    string Content,
    DateTimeOffset? ReceivedAt = null,
    string? ExternalInboundId = null,
    string? ExternalMessageId = null);

public record InboundMessageResultDto(
    int MessageResponseId,
    IntentClassification Classification,
    decimal Confidence,
    int? LeadId,
    bool Suppressed);
