using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Infrastructure.WorkerControl;
using MoneyManager.Observability;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class ScheduledTransactionWorker(
    ILogger<ScheduledTransactionWorker> logger,
    IOptions<WorkerOptions> options,
    IOptions<ScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    WorkerCommandQueueService commandQueue,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;
    private readonly ScheduleOptions _schedule = scheduleOptions.Value;
    private DateTimeOffset? _lastRunAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "TransactionSchedulerWorker INICIADO — Agendado para {Hour:00}:{Minute:00} ({TimeZone}) | Loop: {LoopDelay}s | Timeout: {Timeout}min",
            _schedule.Hour, _schedule.Minute, _schedule.TimeZoneId ?? "Local",
            _schedule.LoopDelaySeconds, _options.ExecutionTimeoutMinutes);

        // Execute immediately on startup to process any backlog
        try
        {
            await RunOnceAsync(timeProvider.GetUtcNow(), stoppingToken, "startup");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "STARTUP EXECUTION falhou. Worker continuará tentando no horário agendado.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var runtimeSchedule = await commandQueue.GetScheduleStateAsync(nameof(ScheduledTransactionWorker));
                var effectiveTimeZoneId = runtimeSchedule?.TimeZoneId ?? _schedule.TimeZoneId;
                var effectiveHour = runtimeSchedule?.Hour ?? _schedule.Hour;
                var effectiveMinute = runtimeSchedule?.Minute ?? _schedule.Minute;
                var effectiveLoopDelaySeconds = runtimeSchedule?.LoopDelaySeconds ?? _schedule.LoopDelaySeconds;

                var claimedCommand = await commandQueue.ClaimNextCommandAsync(nameof(ScheduledTransactionWorker), nameof(ScheduledTransactionWorker));
                if (claimedCommand != null)
                {
                    if (string.Equals(claimedCommand.CommandType, "pause", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(nameof(ScheduledTransactionWorker), true, nameof(ScheduledTransactionWorker));
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "resume", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(nameof(ScheduledTransactionWorker), false, nameof(ScheduledTransactionWorker));
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "run-now", StringComparison.OrdinalIgnoreCase))
                    {
                        var pauseState = await commandQueue.GetPauseStateAsync(nameof(ScheduledTransactionWorker));
                        if (pauseState.IsPaused)
                        {
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, false, "Job is paused");
                        }
                        else
                        {
                            var runNowResult = await RunOnceAsync(timeProvider.GetUtcNow(), stoppingToken, "run-now");
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, runNowResult.Success, runNowResult.ErrorMessage);
                        }
                    }
                }

                var isPaused = (await commandQueue.GetPauseStateAsync(nameof(ScheduledTransactionWorker))).IsPaused;
                if (isPaused)
                {
                    await timeProvider.Delay(TimeSpan.FromSeconds(effectiveLoopDelaySeconds), stoppingToken);
                    continue;
                }

                var nowUtc = timeProvider.GetUtcNow();
                var tz = ResolveTimeZone(effectiveTimeZoneId);
                var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);
                var nextRunLocal = GetNextRunLocal(nowLocal, effectiveHour, effectiveMinute);
                var nextRunUtc = TimeZoneInfo.ConvertTime(nextRunLocal, TimeZoneInfo.Utc);

                if (nowUtc >= nextRunUtc && !AlreadyRanForSlot(nextRunUtc))
                {
                    await RunOnceAsync(nowUtc, stoppingToken, "schedule");
                    _lastRunAt = nextRunUtc;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no loop do agendador.");
            }

            var dynamicDelay = (await commandQueue.GetScheduleStateAsync(nameof(ScheduledTransactionWorker)))?.LoopDelaySeconds ?? _schedule.LoopDelaySeconds;
            await timeProvider.Delay(TimeSpan.FromSeconds(dynamicDelay), stoppingToken);
        }

        logger.LogInformation("TransactionSchedulerWorker finalizado.");
    }

    private async Task<(bool Success, string? ErrorMessage)> RunOnceAsync(DateTimeOffset runStartedAtUtc, CancellationToken stoppingToken, string triggerType)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processLogger = scope.ServiceProvider.GetRequiredService<IProcessLogger>();
        var processor = scope.ServiceProvider.GetRequiredService<ITransactionScheduleProcessor>();

        processLogger.Start("RecurringTransactions", new Dictionary<string, object?>
        {
            ["source"] = "Worker",
            ["worker"] = nameof(ScheduledTransactionWorker),
            ["triggerType"] = triggerType,
            ["triggeredAt"] = runStartedAtUtc.ToString("O")
        });

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

            await processor.ProcessAsync(timeoutCts.Token);
            processLogger.Finish(success: true);
            return (true, null);
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
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            processLogger.Finish(success: false, exception: ex);
            return (false, ex.Message);
        }
    }

    private bool AlreadyRanForSlot(DateTimeOffset slotUtc)
        => _lastRunAt.HasValue && _lastRunAt.Value == slotUtc;

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
        => string.IsNullOrWhiteSpace(timeZoneId) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    private static DateTimeOffset GetNextRunLocal(DateTimeOffset nowLocal, int hour, int minute)
    {
        var today = nowLocal.Date;
        var runToday = new DateTimeOffset(
            today.AddHours(hour).AddMinutes(minute),
            nowLocal.Offset);

        return nowLocal <= runToday
            ? runToday
            : new DateTimeOffset(
                today.AddDays(1).AddHours(hour).AddMinutes(minute),
                nowLocal.Offset);
    }
}
