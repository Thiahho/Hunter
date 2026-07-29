namespace Hunter.Application.Prospecting.Contracts;

public record TagDto(int Id, string Name, string? Color);

public record CreateTagRequest(string Name, string? Color);
