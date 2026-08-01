namespace Hunter.Application.Campaigning.Contracts;

public record SendTestMessageRequest(string Content);

public record TestMessageResultDto(int MessageId, bool Success, string? ExternalMessageId, string? Error);
