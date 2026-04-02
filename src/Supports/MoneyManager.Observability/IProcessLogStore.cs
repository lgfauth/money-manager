namespace MoneyManager.Observability;

public interface IProcessLogStore
{
    void Persist(ProcessLogDocument document);
}

public interface IProcessLogHistoryReader
{
    IReadOnlyList<ProcessExecutionHistoryItem> GetRecent(int limit, string? processName = null);
}

public sealed class ProcessExecutionHistoryItem
{
    public string CorrelationId { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public string? WorkerName { get; set; }
    public DateTime? TriggeredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}