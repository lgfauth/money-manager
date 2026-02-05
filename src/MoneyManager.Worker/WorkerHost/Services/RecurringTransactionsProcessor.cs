using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class RecurringTransactionsProcessor(
    ILogger<RecurringTransactionsProcessor> logger,
    IRecurringTransactionService recurringTransactionService) : ITransactionScheduleProcessor
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        // The current Application implementation does not take a CancellationToken.
        // We still honor cancellation before/after the call.
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("Processando recorrências vencidas...");
        await recurringTransactionService.ProcessDueRecurrencesAsync();
        logger.LogInformation("Processamento de recorrências finalizado.");

        cancellationToken.ThrowIfCancellationRequested();
    }
}
