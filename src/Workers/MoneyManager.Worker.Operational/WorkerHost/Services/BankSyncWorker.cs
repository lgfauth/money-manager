using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionSchedulerWorker.WorkerHost.Options;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class BankSyncWorker(
    ILogger<BankSyncWorker> logger,
    IOptions<BankSyncOptions> options,
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
                var now = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")); // Brasília

                // Roda apenas uma vez por hora-alvo, evitando reprocessamento no mesmo minuto.
                if (_syncHours.Contains(now.Hour) && _lastRunHour != now.Hour)
                {
                    _lastRunHour = now.Hour;
                    logger.LogInformation("BankSyncWorker: iniciando sync das {Hour}h", now.Hour);

                    using var scope = scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<BankSyncProcessor>();
                    await processor.ProcessAsync(stoppingToken);
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
}
