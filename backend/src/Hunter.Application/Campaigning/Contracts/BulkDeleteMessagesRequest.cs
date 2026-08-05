namespace Hunter.Application.Campaigning.Contracts;

public record BulkDeleteMessagesRequest(IReadOnlyCollection<int> Ids);
