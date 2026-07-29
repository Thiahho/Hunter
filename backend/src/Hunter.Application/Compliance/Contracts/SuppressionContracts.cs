using Hunter.Domain.Compliance;

namespace Hunter.Application.Compliance.Contracts;

public record SuppressionDto(int Id, string Contact, SuppressionContactType ContactType, SuppressionReason Reason, string? Source, DateTimeOffset CreatedAt);

public record CreateSuppressionRequest(string Contact, SuppressionContactType ContactType, SuppressionReason Reason, string? Source = null);
