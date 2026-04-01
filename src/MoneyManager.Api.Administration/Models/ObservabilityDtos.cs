namespace MoneyManager.Api.Administration.Models;

public sealed class SystemStatusResponse
{
    public string ApiStatus { get; set; } = "healthy";
    public string MongoStatus { get; set; } = "unknown";
    public string WorkerStatus { get; set; } = "unknown";
    public DateTime TimestampUtc { get; set; }
    public string Environment { get; set; } = string.Empty;
}

public sealed class JobHistoryItem
{
    public string JobName { get; set; } = string.Empty;
    public string LastStatus { get; set; } = "unknown";
    public DateTime? LastRunAtUtc { get; set; }
    public long? LastDurationMs { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class JobExecutionHistoryEntry
{
    public string CorrelationId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public string? WorkerName { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class MetricsSummaryResponse
{
    public DateTime WindowStartedAtUtc { get; set; }
    public DateTime WindowEndedAtUtc { get; set; }
    public int Http5xxCount { get; set; }
    public int Http4xxCount { get; set; }
    public double? ApiP95Ms { get; set; }
    public int JobFailures { get; set; }
}
