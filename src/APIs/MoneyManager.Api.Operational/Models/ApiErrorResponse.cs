namespace MoneyManager.Presentation.Models;

public sealed class ApiErrorResponse
{
    public int StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public List<string> Errors { get; init; } = [];

    public string TraceId { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string? Details { get; init; }
}