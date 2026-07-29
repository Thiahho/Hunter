namespace Hunter.Shared;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyCollection<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string message, IReadOnlyCollection<string>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
