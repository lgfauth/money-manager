using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class RecurringTransactionsProcessor(
    ILogger<RecurringTransactionsProcessor> logger,
    IRecurringTransactionService recurringTransactionService,
    ICreditCardInvoiceService invoiceService) : ITransactionScheduleProcessor
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

        logger.LogInformation("Processando fechamento de faturas de cartão de crédito...");
        await invoiceService.ProcessMonthlyInvoiceClosuresAsync();
        logger.LogInformation("Processamento de fechamento de faturas finalizado.");

        cancellationToken.ThrowIfCancellationRequested();
    }
}
