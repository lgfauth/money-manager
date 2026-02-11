using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Worker dedicado ao fechamento automático de faturas de cartão de crédito
/// Executa diariamente à meia-noite e 1 minuto (00:01)
/// </summary>
internal sealed class InvoiceClosureWorker(
    ILogger<InvoiceClosureWorker> logger,
    IOptions<WorkerOptions> options,
    IOptions<InvoiceClosureScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    InvoiceClosureProcessor processor) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;
    private readonly InvoiceClosureScheduleOptions _schedule = scheduleOptions.Value;
    private DateTimeOffset? _lastRunAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("========================================");
        logger.LogInformation("InvoiceClosureWorker INICIADO");
        logger.LogInformation("Agendado para {Hour:00}:{Minute:00} (TimeZone: {TimeZone})",
            _schedule.Hour,
            _schedule.Minute,
            _schedule.TimeZoneId ?? "Local");
        logger.LogInformation("Loop delay: {LoopDelay}s | Timeout: {Timeout}min",
            _schedule.LoopDelaySeconds,
            _options.ExecutionTimeoutMinutes);
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
                    "Invoice Closure Check: Now={NowLocal:yyyy-MM-dd HH:mm:ss} | NextRun={NextRun:yyyy-MM-dd HH:mm:ss} | AlreadyRan={AlreadyRan}",
                    nowLocal,
                    nextRunLocal,
                    AlreadyRanForSlot(nextRunUtc));

                // Se chegou a hora e ainda não executou neste slot
                if (nowUtc >= nextRunUtc && !AlreadyRanForSlot(nextRunUtc))
                {
                    logger.LogInformation("TRIGGER: Executando fechamento de faturas (hora agendada atingida)");
                    await RunOnceAsync(nowUtc, stoppingToken);
                    _lastRunAt = nextRunUtc;
                    logger.LogInformation("Fechamento concluído. Próxima execução: {NextRun:yyyy-MM-dd HH:mm:ss}",
                        GetNextRunLocal(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), tz), _schedule));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("InvoiceClosureWorker cancelado por desligamento.");
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
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

            logger.LogInformation("Iniciando fechamento de faturas em {StartedAtUtc}", runStartedAtUtc);
            await processor.ProcessAsync(timeoutCts.Token);
            logger.LogInformation("Fechamento de faturas finalizado em {FinishedAtUtc}", timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Fechamento de faturas cancelado por timeout ({TimeoutMinutes} min).", _options.ExecutionTimeoutMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no fechamento de faturas.");
        }
    }

    private bool AlreadyRanForSlot(DateTimeOffset slotUtc)
        => _lastRunAt.HasValue && _lastRunAt.Value == slotUtc;

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Local;
        }

        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    private static DateTimeOffset GetNextRunLocal(DateTimeOffset nowLocal, InvoiceClosureScheduleOptions schedule)
    {
        var today = nowLocal.Date;
        var runToday = new DateTimeOffset(
            today.AddHours(schedule.Hour).AddMinutes(schedule.Minute),
            nowLocal.Offset);

        if (nowLocal <= runToday)
        {
            return runToday;
        }

        var tomorrow = today.AddDays(1);
        return new DateTimeOffset(
            tomorrow.AddHours(schedule.Hour).AddMinutes(schedule.Minute),
            nowLocal.Offset);
    }
}
