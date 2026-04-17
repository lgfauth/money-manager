using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Infrastructure.Data;
using MoneyManager.Observability;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AdminPolicies.Viewer)]
public sealed class AdminObservabilityController : ControllerBase
{
    private static readonly (string JobName, string ProcessName)[] JobCatalog =
    [
        ("ScheduledTransactionWorker", "RecurringTransactions"),
        ("CreditCardInvoiceWorker", "CreditCardInvoiceStatus"),
        ("DailyReminderWorker", "DailyReminder")
    ];

    private readonly MongoContext _mongoContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IProcessLogHistoryReader _processLogHistoryReader;

    public AdminObservabilityController(
        MongoContext mongoContext,
        IWebHostEnvironment environment,
        IProcessLogHistoryReader processLogHistoryReader)
    {
        _mongoContext = mongoContext;
        _environment = environment;
        _processLogHistoryReader = processLogHistoryReader;
    }

    [HttpGet("system-status")]
    public async Task<ActionResult<SystemStatusResponse>> GetSystemStatus()
    {
        var mongoStatus = "healthy";

        try
        {
            await _mongoContext.TestConnectionAsync();
        }
        catch
        {
            mongoStatus = "degraded";
        }

        return Ok(new SystemStatusResponse
        {
            ApiStatus = "healthy",
            MongoStatus = mongoStatus,
            WorkerStatus = "unknown",
            TimestampUtc = DateTime.UtcNow,
            Environment = _environment.EnvironmentName
        });
    }

    [HttpGet("jobs/history")]
    public ActionResult<IReadOnlyList<JobHistoryItem>> GetJobsHistory()
    {
        var items = JobCatalog
            .Select(job =>
            {
                var latest = _processLogHistoryReader.GetRecent(1, job.ProcessName).FirstOrDefault();

                return new JobHistoryItem
                {
                    JobName = job.JobName,
                    LastStatus = latest?.Status ?? "unknown",
                    LastRunAtUtc = latest?.StartedAtUtc,
                    LastDurationMs = latest?.DurationMs,
                    Notes = latest is null
                        ? "Nenhuma execucao persistida ainda."
                        : latest.ErrorMessage ?? $"Ultima execucao registrada por {latest.WorkerName ?? job.JobName}."
                };
            })
            .ToList();

        return Ok(items);
    }

    [HttpGet("jobs/{jobName}/history")]
    public ActionResult<IReadOnlyList<JobExecutionHistoryEntry>> GetJobHistory(string jobName, [FromQuery] int limit = 20)
    {
        var mapping = ResolveJob(jobName);
        if (mapping is null)
        {
            return NotFound(new { message = $"Job '{jobName}' nao encontrado" });
        }

        var entries = _processLogHistoryReader
            .GetRecent(limit, mapping.Value.ProcessName)
            .Select(item => new JobExecutionHistoryEntry
            {
                CorrelationId = item.CorrelationId,
                JobName = mapping.Value.JobName,
                Status = item.Status,
                StartedAtUtc = item.StartedAtUtc,
                FinishedAtUtc = item.FinishedAtUtc,
                DurationMs = item.DurationMs,
                WorkerName = item.WorkerName,
                TriggeredAtUtc = item.TriggeredAtUtc,
                ErrorMessage = item.ErrorMessage
            })
            .ToList();

        return Ok(entries);
    }

    [HttpGet("metrics/summary")]
    public ActionResult<MetricsSummaryResponse> GetMetricsSummary()
    {
        var now = DateTime.UtcNow;

        var summary = new MetricsSummaryResponse
        {
            WindowStartedAtUtc = now.AddHours(-24),
            WindowEndedAtUtc = now,
            Http5xxCount = 0,
            Http4xxCount = 0,
            ApiP95Ms = null,
            JobFailures = 0
        };

        return Ok(summary);
    }

    private static (string JobName, string ProcessName)? ResolveJob(string jobName)
    {
        foreach (var job in JobCatalog)
        {
            if (string.Equals(job.JobName, jobName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(job.ProcessName, jobName, StringComparison.OrdinalIgnoreCase))
            {
                return job;
            }
        }

        return null;
    }
}
