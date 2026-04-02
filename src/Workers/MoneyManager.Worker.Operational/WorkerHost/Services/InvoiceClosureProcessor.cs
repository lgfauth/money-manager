using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class InvoiceClosureProcessor(
    IProcessLogger processLogger,
    ICreditCardInvoiceService invoiceService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Iniciando fechamento de faturas");

        await invoiceService.ProcessMonthlyInvoiceClosuresAsync();

        processLogger.AddStep("Fechamento de faturas processado com sucesso");

        cancellationToken.ThrowIfCancellationRequested();
    }
}
