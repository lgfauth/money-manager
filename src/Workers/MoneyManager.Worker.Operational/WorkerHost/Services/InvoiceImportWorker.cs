using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Infrastructure.WorkerControl;
using MoneyManager.Observability;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Processa a fila de importações de fatura de cartão a cada <see cref="InvoiceImportOptions.PollIntervalSeconds"/>.
/// Só registra execução no IProcessLogger quando há trabalho (ou em run-now), para não poluir os logs.
/// Aceita os comandos administrativos run-now / pause / resume.
/// </summary>
internal sealed class InvoiceImportWorker(
    ILogger<InvoiceImportWorker> logger,
    IOptions<InvoiceImportOptions> options,
    ITimeProvider timeProvider,
    WorkerCommandQueueService commandQueue,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private const string JobName = nameof(InvoiceImportWorker);
    private readonly InvoiceImportOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InvoiceImportWorker INICIADO — verificação a cada {Interval}s", _options.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedCommand = await commandQueue.ClaimNextCommandAsync(JobName, JobName);
                if (claimedCommand != null)
                {
                    if (string.Equals(claimedCommand.CommandType, "pause", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(JobName, true, JobName);
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "resume", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(JobName, false, JobName);
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "run-now", StringComparison.OrdinalIgnoreCase))
                    {
                        var pauseState = await commandQueue.GetPauseStateAsync(JobName);
                        if (pauseState.IsPaused)
                        {
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, false, "Job is paused");
                        }
                        else
                        {
                            var (success, errorMessage) = await RunOnceAsync(stoppingToken, "run-now", force: true);
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, success, errorMessage);
                        }
                    }
                }

                var isPaused = (await commandQueue.GetPauseStateAsync(JobName)).IsPaused;
                if (!isPaused)
                    await RunOnceAsync(stoppingToken, "scheduled", force: false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Falha em um ciclo não pode derrubar o host
                logger.LogError(ex, "Erro inesperado no loop do InvoiceImportWorker.");
            }

            await timeProvider.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> RunOnceAsync(CancellationToken stoppingToken, string triggerType, bool force)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var processor = scope.ServiceProvider.GetRequiredService<InvoiceImportProcessor>();

        // Fila vazia: nada a registrar
        if (!force && !await processor.HasPendingWorkAsync())
            return (true, null);

        var processLogger = scope.ServiceProvider.GetRequiredService<IProcessLogger>();
        processLogger.Start("InvoiceImport", new Dictionary<string, object?>
        {
            ["source"] = "Worker",
            ["worker"] = JobName,
            ["triggerType"] = triggerType,
            ["triggeredAt"] = timeProvider.GetUtcNow().ToString("O")
        });

        try
        {
            await processor.ProcessAsync(stoppingToken);
            processLogger.Finish(success: true);
            return (true, null);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            processLogger.AddWarning("Processo cancelado por desligamento do host");
            processLogger.Finish(success: false);
            throw;
        }
        catch (Exception ex)
        {
            processLogger.Finish(success: false, exception: ex);
            logger.LogError(ex, "Erro ao processar importações de fatura");
            return (false, ex.Message);
        }
    }
}
