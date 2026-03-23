using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Dispara o lembrete push diário às 21h (horário de Brasília por padrão).
/// Não executa no startup — o lembrete só faz sentido no horário certo.
/// </summary>
internal sealed class DailyReminderWorker(
    ILogger<DailyReminderWorker> logger,
    IOptions<WorkerOptions> options,
    IOptions<DailyReminderScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;
    private readonly DailyReminderScheduleOptions _schedule = scheduleOptions.Value;
    private DateTimeOffset? _lastRunAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("========================================");
        logger.LogInformation("DailyReminderWorker INICIADO");
        logger.LogInformation("Agendado para {Hour:00}:{Minute:00} (TimeZone: {TimeZone})",
            _schedule.Hour,
            _schedule.Minute,
            _schedule.TimeZoneId ?? "Local");
        logger.LogInformation("========================================");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = timeProvider.GetUtcNow();
                var tz = ResolveTimeZone(_schedule.TimeZoneId);
                var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);

                var nextRunLocal = GetNextRunLocal(nowLocal, _schedule);
                var nextRunUtc = TimeZoneInfo.ConvertTime(nextRunLocal, TimeZoneInfo.Utc);

                logger.LogDebug(
                    "DailyReminder check: Now={NowLocal:yyyy-MM-dd HH:mm:ss} | NextRun={NextRun:yyyy-MM-dd HH:mm:ss} | AlreadyRan={AlreadyRan}",
                    nowLocal, nextRunLocal, AlreadyRanForSlot(nextRunUtc));

                if (nowUtc >= nextRunUtc && !AlreadyRanForSlot(nextRunUtc))
                {
                    logger.LogInformation("TRIGGER: Enviando lembretes diários...");
                    await RunOnceAsync(nowUtc, stoppingToken);
                    _lastRunAt = nextRunUtc;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("DailyReminderWorker cancelado por desligamento.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no loop do DailyReminderWorker.");
            }

            await timeProvider.Delay(TimeSpan.FromSeconds(_schedule.LoopDelaySeconds), stoppingToken);
        }

        logger.LogInformation("DailyReminderWorker finalizado.");
    }

    private async Task RunOnceAsync(DateTimeOffset runStartedAtUtc, CancellationToken stoppingToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

            // Processor é Scoped — precisa de scope próprio
            await using var scope = scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<DailyReminderProcessor>();

            logger.LogInformation("Iniciando envio de lembretes em {StartedAtUtc}", runStartedAtUtc);
            await processor.ProcessAsync(timeoutCts.Token);
            logger.LogInformation("Lembretes enviados em {FinishedAtUtc}", timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Envio de lembretes cancelado por timeout ({TimeoutMinutes} min).", _options.ExecutionTimeoutMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no envio de lembretes.");
        }
    }

    private bool AlreadyRanForSlot(DateTimeOffset slotUtc)
        => _lastRunAt.HasValue && _lastRunAt.Value == slotUtc;

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
        => string.IsNullOrWhiteSpace(timeZoneId)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    private static DateTimeOffset GetNextRunLocal(DateTimeOffset nowLocal, DailyReminderScheduleOptions schedule)
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
