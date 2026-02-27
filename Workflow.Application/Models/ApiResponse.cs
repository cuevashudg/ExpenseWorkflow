namespace Workflow.Application.Models;

/// <summary>
/// Standard API response wrapper for all endpoints.
/// Every response returns { success, data?, error? } for consistency.
/// </summary>
public class ApiResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public static ApiResponse Ok() => new() { Success = true };
    public static ApiResponse Fail(string error) => new() { Success = false, Error = error };
}

/// <summary>
/// Generic API response wrapper with typed data payload.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
}
