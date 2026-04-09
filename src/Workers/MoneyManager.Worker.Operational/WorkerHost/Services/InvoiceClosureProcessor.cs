using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class InvoiceClosureProcessor(
    IProcessLogger processLogger,
    ICreditCardInvoiceService invoiceService)
{
    public async Task ProcessAsync(DateTime referenceDateLocal, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Iniciando fechamento de faturas");

        await invoiceService.ProcessMonthlyInvoiceClosuresAsync(referenceDateLocal);

        processLogger.AddStep("Fechamento de faturas processado com sucesso");

        cancellationToken.ThrowIfCancellationRequested();
    }
}
