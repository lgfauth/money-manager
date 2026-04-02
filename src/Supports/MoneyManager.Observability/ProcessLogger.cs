using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Observability;

public sealed class ProcessLogger : IProcessLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly ILogger<ProcessLogger> _logger;
    private readonly IEnumerable<IProcessLogStore> _stores;
    private readonly Stopwatch _stopwatch = new();
    private ProcessLogDocument? _document;

    public ProcessLogger(ILogger<ProcessLogger> logger, IEnumerable<IProcessLogStore> stores)
    {
        _logger = logger;
        _stores = stores;
    }

    public void Start(string processName, Dictionary<string, object?>? context = null)
    {
        _document = new ProcessLogDocument
        {
            CorrelationId = Guid.NewGuid().ToString("N")[..12],
            ProcessName = processName,
            StartedAt = DateTime.UtcNow,
            Context = context
        };

        _stopwatch.Restart();
    }

    public void AddContext(string key, object? value)
    {
        EnsureStarted();
        _document!.Context ??= new Dictionary<string, object?>();
        _document.Context[key] = value;
    }

    public void AddStep(string message, Dictionary<string, object?>? data = null)
    {
        EnsureStarted();
        _document!.Steps.Add(new LogStep
        {
            At = _stopwatch.ElapsedMilliseconds,
            Level = "Information",
            Message = message,
            Data = data
        });
    }

    public void AddWarning(string message, Dictionary<string, object?>? data = null)
    {
        EnsureStarted();
        _document!.Steps.Add(new LogStep
        {
            At = _stopwatch.ElapsedMilliseconds,
            Level = "Warning",
            Message = message,
            Data = data
        });
    }

    public void AddError(string message, Exception? exception = null, Dictionary<string, object?>? data = null)
    {
        EnsureStarted();

        var step = new LogStep
        {
            At = _stopwatch.ElapsedMilliseconds,
            Level = "Error",
            Message = message,
            Data = data
        };

        if (exception != null && step.Data == null)
        {
            step.Data = new Dictionary<string, object?>
            {
                ["exceptionType"] = exception.GetType().Name,
                ["exceptionMessage"] = exception.Message
            };
        }

        _document!.Steps.Add(step);
    }

    public void Finish(bool success = true, Exception? exception = null)
    {
        EnsureStarted();
        _stopwatch.Stop();

        _document!.FinishedAt = DateTime.UtcNow;
        _document.DurationMs = _stopwatch.ElapsedMilliseconds;
        _document.Status = exception != null ? "Failed" : success ? "Success" : "Failed";

        if (exception != null)
        {
            _document.Exception = new ExceptionInfo
            {
                Type = exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace
            };
        }

        Flush();
    }

    private void Flush()
    {
        foreach (var store in _stores)
        {
            store.Persist(_document!);
        }

        var json = JsonSerializer.Serialize(_document, JsonOptions);

        var logLevel = _document!.Status == "Success" ? LogLevel.Information : LogLevel.Error;

        _logger.Log(logLevel,
            "[ProcessLog] {ProcessName} | {Status} | {DurationMs}ms | {ProcessLogJson}",
            _document.ProcessName,
            _document.Status,
            _document.DurationMs,
            json);
    }

    private void EnsureStarted()
    {
        if (_document == null)
            throw new InvalidOperationException("ProcessLogger has not been started. Call Start() first.");
    }
}
