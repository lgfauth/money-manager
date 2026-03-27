using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Observability;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class InvoiceClosureWorker(
    ILogger<InvoiceClosureWorker> logger,
    IOptions<WorkerOptions> options,
    IOptions<InvoiceClosureScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;
    private readonly InvoiceClosureScheduleOptions _schedule = scheduleOptions.Value;
    private DateTimeOffset? _lastRunAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "InvoiceClosureWorker INICIADO — Agendado para {Hour:00}:{Minute:00} ({TimeZone}) | Loop: {LoopDelay}s | Timeout: {Timeout}min",
            _schedule.Hour, _schedule.Minute, _schedule.TimeZoneId ?? "Local",
            _schedule.LoopDelaySeconds, _options.ExecutionTimeoutMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = timeProvider.GetUtcNow();
                var tz = ResolveTimeZone(_schedule.TimeZoneId);
                var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);
                var nextRunLocal = GetNextRunLocal(nowLocal, _schedule);
                var nextRunUtc = TimeZoneInfo.ConvertTime(nextRunLocal, TimeZoneInfo.Utc);

                if (nowUtc >= nextRunUtc && !AlreadyRanForSlot(nextRunUtc))
                {
                    await RunOnceAsync(nowUtc, stoppingToken);
                    _lastRunAt = nextRunUtc;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no loop do InvoiceClosureWorker.");
            }

            await timeProvider.Delay(TimeSpan.FromSeconds(_schedule.LoopDelaySeconds), stoppingToken);
        }

        logger.LogInformation("InvoiceClosureWorker finalizado.");
    }

    private async Task RunOnceAsync(DateTimeOffset runStartedAtUtc, CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processLogger = scope.ServiceProvider.GetRequiredService<IProcessLogger>();
        var processor = scope.ServiceProvider.GetRequiredService<InvoiceClosureProcessor>();

        processLogger.Start("InvoiceClosure", new Dictionary<string, object?>
        {
            ["source"] = "Worker",
            ["worker"] = nameof(InvoiceClosureWorker),
            ["triggeredAt"] = runStartedAtUtc.ToString("O")
        });

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

            await processor.ProcessAsync(timeoutCts.Token);
            processLogger.Finish(success: true);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            processLogger.AddWarning("Processo cancelado por desligamento do host");
            processLogger.Finish(success: false);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            processLogger.AddWarning($"Processo cancelado por timeout ({_options.ExecutionTimeoutMinutes} min)");
            processLogger.Finish(success: false, exception: ex);
        }
        catch (Exception ex)
        {
            processLogger.Finish(success: false, exception: ex);
        }
    }

    private bool AlreadyRanForSlot(DateTimeOffset slotUtc)
        => _lastRunAt.HasValue && _lastRunAt.Value == slotUtc;

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
        => string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    private static DateTimeOffset GetNextRunLocal(DateTimeOffset nowLocal, InvoiceClosureScheduleOptions schedule)
    {
        var today = nowLocal.Date;
        var runToday = new DateTimeOffset(
            today.AddHours(schedule.Hour).AddMinutes(schedule.Minute),
            nowLocal.Offset);

        return nowLocal <= runToday
            ? runToday
            : new DateTimeOffset(
                today.AddDays(1).AddHours(schedule.Hour).AddMinutes(schedule.Minute),
                nowLocal.Offset);
    }
}
