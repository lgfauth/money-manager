using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class RecurringTransactionsProcessor(
    IProcessLogger processLogger,
    IRecurringTransactionService recurringTransactionService) : ITransactionScheduleProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Processando recorrências vencidas");
        var summary = await recurringTransactionService.ProcessDueRecurrencesAsync();
        processLogger.AddStep("Processamento de recorrências finalizado", new Dictionary<string, object?>
        {
            ["usersAffected"] = summary.ProcessedByUser.Count,
            ["totalProcessed"] = summary.TotalProcessed
        });
    }
}
