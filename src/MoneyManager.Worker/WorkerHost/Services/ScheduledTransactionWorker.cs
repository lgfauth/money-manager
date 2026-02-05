using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Worker responsável por executar o processamento de agendamentos a cada X horas.
/// Estrutura seguindo boas práticas:
/// - BackgroundService
/// - Cancelamento cooperativo
/// - Timeout por execução
/// - Separação de responsabilidades (orquestração vs. processamento)
/// </summary>
internal sealed class ScheduledTransactionWorker(
    ILogger<ScheduledTransactionWorker> logger,
    IOptions<WorkerOptions> options,
    ITimeProvider timeProvider,
    ITransactionScheduleProcessor processor) : BackgroundService
{
    private readonly WorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("TransactionSchedulerWorker iniciado. Intervalo: {IntervalHours}h", _options.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            var runStartedAt = timeProvider.GetUtcNow();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.ExecutionTimeoutMinutes));

                logger.LogInformation("Iniciando processamento de agendamentos em {StartedAtUtc}", runStartedAt);
                await processor.ProcessAsync(timeoutCts.Token);
                logger.LogInformation("Processamento finalizado em {FinishedAtUtc}", timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown
                logger.LogInformation("Worker cancelado por desligamento.");
                break;
            }
            catch (OperationCanceledException)
            {
                // Timeout por execução
                logger.LogWarning("Execução cancelada por timeout ({TimeoutMinutes} min).", _options.ExecutionTimeoutMinutes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado no processamento de agendamentos.");
            }

            // Aguarda próximo ciclo (a cada 3h por padrão)
            var delay = TimeSpan.FromHours(_options.IntervalHours);
            logger.LogInformation("Próxima execução em {Delay}.", delay);
            await timeProvider.Delay(delay, stoppingToken);
        }

        logger.LogInformation("TransactionSchedulerWorker finalizado.");
    }
}
