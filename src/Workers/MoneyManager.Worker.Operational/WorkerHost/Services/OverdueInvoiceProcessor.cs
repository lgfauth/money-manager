using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class OverdueInvoiceProcessor(
    IProcessLogger processLogger,
    ICreditCardInvoiceService invoiceService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Iniciando marcacao de faturas vencidas");

        var updatedCount = await invoiceService.MarkOverdueInvoicesAsync();

        processLogger.AddStep("Marcacao de faturas vencidas concluida", new Dictionary<string, object?>
        {
            ["updatedCount"] = updatedCount
        });

        cancellationToken.ThrowIfCancellationRequested();
    }
}
