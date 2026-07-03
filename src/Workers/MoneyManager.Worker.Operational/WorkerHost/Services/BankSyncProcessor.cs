using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class BankSyncProcessor(
    IProcessLogger processLogger,
    IBankConnectionService bankConnectionService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        processLogger.AddStep("Iniciando sync bancário periódico");
        await bankConnectionService.SyncAllActiveConnectionsAsync(cancellationToken);
        processLogger.AddStep("Sync bancário periódico finalizado");
    }
}
