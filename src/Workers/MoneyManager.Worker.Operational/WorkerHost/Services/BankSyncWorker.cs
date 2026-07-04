using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Infrastructure.WorkerControl;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class BankSyncWorker(
    ILogger<BankSyncWorker> logger,
    IOptions<BankSyncOptions> options,
    WorkerCommandQueueService commandQueue,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly BankSyncOptions _options = options.Value;
    private readonly HashSet<int> _syncHours = [..options.Value.SyncHours];
    private int? _lastRunHour = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Comandos administrativos (run-now/pause/resume) vindos do Backoffice.
                var claimedCommand = await commandQueue.ClaimNextCommandAsync(nameof(BankSyncWorker), nameof(BankSyncWorker));
                if (claimedCommand != null)
                {
                    if (string.Equals(claimedCommand.CommandType, "pause", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(nameof(BankSyncWorker), true, nameof(BankSyncWorker));
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "resume", StringComparison.OrdinalIgnoreCase))
                    {
                        await commandQueue.SetPausedStateAsync(nameof(BankSyncWorker), false, nameof(BankSyncWorker));
                        await commandQueue.CompleteAsync(claimedCommand.CommandId, true, null);
                    }
                    else if (string.Equals(claimedCommand.CommandType, "run-now", StringComparison.OrdinalIgnoreCase))
                    {
                        var pauseState = await commandQueue.GetPauseStateAsync(nameof(BankSyncWorker));
                        if (pauseState.IsPaused)
                        {
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, false, "Job is paused");
                        }
                        else
                        {
                            logger.LogInformation("BankSyncWorker: sync disparado via run-now administrativo");
                            var (success, errorMessage) = await RunOnceAsync(stoppingToken);
                            await commandQueue.CompleteAsync(claimedCommand.CommandId, success, errorMessage);
                        }
                    }
                }

                var isPaused = (await commandQueue.GetPauseStateAsync(nameof(BankSyncWorker))).IsPaused;

                var now = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")); // Brasília

                // Roda apenas uma vez por hora-alvo, evitando reprocessamento no mesmo minuto.
                if (!isPaused && _syncHours.Contains(now.Hour) && _lastRunHour != now.Hour)
                {
                    _lastRunHour = now.Hour;
                    logger.LogInformation("BankSyncWorker: iniciando sync das {Hour}h", now.Hour);
                    await RunOnceAsync(stoppingToken);
                }

                // Reset do controle ao virar a hora.
                if (_lastRunHour.HasValue && _lastRunHour != now.Hour && !_syncHours.Contains(now.Hour))
                    _lastRunHour = null;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Falha em um ciclo não pode derrubar o host (StopHost pararia todos os jobs).
                logger.LogError(ex, "Erro inesperado no loop do BankSyncWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.LoopDelaySeconds), stoppingToken);
        }
    }

    private async Task<(bool Success, string? ErrorMessage)> RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<BankSyncProcessor>();
            await processor.ProcessAsync(stoppingToken);
            return (true, null);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar sync bancário");
            return (false, ex.Message);
        }
    }
}
