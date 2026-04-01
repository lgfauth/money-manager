using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Observability;

namespace MoneyManager.Infrastructure.Observability;

public sealed class MongoProcessLogStore : IProcessLogStore, IProcessLogHistoryReader
{
    private readonly IMongoCollection<PersistedProcessLogEntry> _collection;

    public MongoProcessLogStore(MongoContext mongoContext)
    {
        _collection = mongoContext.GetCollection<PersistedProcessLogEntry>("worker_process_logs");
    }

    public void Persist(ProcessLogDocument document)
    {
        var entry = new PersistedProcessLogEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            CorrelationId = document.CorrelationId,
            ProcessName = document.ProcessName,
            Status = document.Status,
            StartedAtUtc = document.StartedAt,
            FinishedAtUtc = document.FinishedAt,
            DurationMs = document.DurationMs,
            WorkerName = GetString(document.Context, "worker"),
            TriggeredAtUtc = ParseDateTime(GetString(document.Context, "triggeredAt")),
            ErrorMessage = document.Exception?.Message,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(document)
        };

        _collection.InsertOne(entry);
    }

    public IReadOnlyList<ProcessExecutionHistoryItem> GetRecent(int limit, string? processName = null)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 200);
        var filter = string.IsNullOrWhiteSpace(processName)
            ? Builders<PersistedProcessLogEntry>.Filter.Empty
            : Builders<PersistedProcessLogEntry>.Filter.Eq(x => x.ProcessName, processName);

        return _collection
            .Find(filter)
            .SortByDescending(x => x.StartedAtUtc)
            .Limit(effectiveLimit)
            .ToList()
            .Select(x => new ProcessExecutionHistoryItem
            {
                CorrelationId = x.CorrelationId,
                ProcessName = x.ProcessName,
                Status = x.Status,
                StartedAtUtc = x.StartedAtUtc,
                FinishedAtUtc = x.FinishedAtUtc,
                DurationMs = x.DurationMs,
                WorkerName = x.WorkerName,
                TriggeredAtUtc = x.TriggeredAtUtc,
                ErrorMessage = x.ErrorMessage
            })
            .ToList();
    }

    private static string? GetString(Dictionary<string, object?>? context, string key)
    {
        if (context is null || !context.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value.ToString();
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }

    private sealed class PersistedProcessLogEntry
    {
        [BsonId]
        public string Id { get; set; } = string.Empty;

        public string CorrelationId { get; set; } = string.Empty;

        public string ProcessName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public DateTime? FinishedAtUtc { get; set; }

        public long DurationMs { get; set; }

        public string? WorkerName { get; set; }

        public DateTime? TriggeredAtUtc { get; set; }

        public string? ErrorMessage { get; set; }

        public string PayloadJson { get; set; } = string.Empty;
    }
}