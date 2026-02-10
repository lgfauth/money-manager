using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Worker responsável por executar o processamento agendado (1x ao dia).
/// Estrutura seguindo boas práticas:
/// - BackgroundService
/// - Cancelamento cooperativo
/// - Timeout por execução
/// - Separação de responsabilidades (orquestração vs. processamento)
/// </summary>
internal sealed class ScheduledTransactionWorker(
    ILogger<ScheduledTransactionWorker> logger,
    IOptions<WorkerOptions> options,
    IOptions<ScheduleOptions> scheduleOptions,
    ITimeProvider timeProvider,
    ITransactionScheduleProcessor processor) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;
    private readonly ScheduleOptions _schedule = scheduleOptions.Value;
    private DateTimeOffset? _lastRunAt;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "========================================");
        logger.LogInformation(
            "TransactionSchedulerWorker INICIADO");
        logger.LogInformation(
            "Agendado para {Hour:00}:{Minute:00} (TimeZone: {TimeZone})",
            _schedule.Hour,
            _schedule.Minute,
            _schedule.TimeZoneId ?? "Local");
        logger.LogInformation(
            "Loop delay: {LoopDelay}s | Timeout: {Timeout}min",
            _schedule.LoopDelaySeconds,
            _options.ExecutionTimeoutMinutes);
        logger.LogInformation("========================================");

        // Execute immediately on startup to process any backlog and validate everything works
        logger.LogInformation("STARTUP EXECUTION: Processando recorrências vencidas imediatamente...");
        try
        {
            await RunOnceAsync(timeProvider.GetUtcNow(), stoppingToken);
            logger.LogInformation("STARTUP EXECUTION: Concluída com sucesso. Aguardando próximo horário agendado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "STARTUP EXECUTION: Falhou. Worker continuará tentando no horário agendado.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nowUtc = timeProvider.GetUtcNow();
                var tz = ResolveTimeZone(_schedule.TimeZoneId);
                var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, tz);

                var nextRunLocal = GetNextRunLocal(nowLocal, _schedule);
                var nextRunUtc = TimeZoneInfo.ConvertTime(nextRunLocal, TimeZoneInfo.Utc);

                // Log scheduling info every loop iteration (for debugging)
                logger.LogDebug(
                    "Schedule check: Now={NowLocal:yyyy-MM-dd HH:mm:ss} | NextRun={NextRun:yyyy-MM-dd HH:mm:ss} | AlreadyRan={AlreadyRan}",
                    nowLocal,
                    nextRunLocal,
                    AlreadyRanForSlot(nextRunUtc));

                // If it's time (or we're late) and we didn't run for this slot yet
                if (nowUtc >= nextRunUtc && !AlreadyRanForSlot(nextRunUtc))
                {
                    logger.LogInformation(
                        "TRIGGER: Executando processamento agendado (Now >= NextRun and not already ran)");
                    await RunOnceAsync(nowUtc, stoppingToken);
                    _lastRunAt = nextRunUtc;
                    logger.LogInformation("Processamento agendado concluído. Próxima execução: {NextRun:yyyy-MM-dd HH:mm:ss}",
                        GetNextRunLocal(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), tz), _schedule));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Worker cancelado por desligamento.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no loop do agendador.");
            }

            await timeProvider.Delay(TimeSpan.FromSeconds(_schedule.LoopDelaySeconds), stoppingToken);
        }

        logger.LogInformation("TransactionSchedulerWorker finalizado.");
    }

    private async Task RunOnceAsync(DateTimeOffset runStartedAtUtc, CancellationToken stoppingToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

            logger.LogInformation("Iniciando processamento em {StartedAtUtc}", runStartedAtUtc);
            await processor.ProcessAsync(timeoutCts.Token);
            logger.LogInformation("Processamento finalizado em {FinishedAtUtc}", timeProvider.GetUtcNow());
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Execução cancelada por timeout ({TimeoutMinutes} min).", _options.ExecutionTimeoutMinutes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado no processamento.");
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

    private static DateTimeOffset GetNextRunLocal(DateTimeOffset nowLocal, ScheduleOptions schedule)
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
