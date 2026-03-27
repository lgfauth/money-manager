using System.Text.Json.Serialization;

namespace MoneyManager.Observability;

public sealed class ProcessLogDocument
{
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("processName")]
    public string ProcessName { get; set; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }

    [JsonPropertyName("finishedAt")]
    public DateTime? FinishedAt { get; set; }

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Running";

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Context { get; set; }

    [JsonPropertyName("steps")]
    public List<LogStep> Steps { get; set; } = [];

    [JsonPropertyName("exception")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExceptionInfo? Exception { get; set; }
}

public sealed class ExceptionInfo
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("stackTrace")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackTrace { get; set; }
}
