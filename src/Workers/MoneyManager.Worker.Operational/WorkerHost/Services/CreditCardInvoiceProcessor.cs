using MoneyManager.Application.Services;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class CreditCardInvoiceProcessor(
    IProcessLogger processLogger,
    ICreditCardInvoiceService invoiceService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        processLogger.AddStep("Updating credit card invoice statuses");

        var summary = await invoiceService.PromotePendingAndMarkOverdueAsync();

        processLogger.AddStep("Credit card invoice status update complete", new Dictionary<string, object?>
        {
            ["promotedToOpen"] = summary.PromotedToOpen,
            ["closedInvoices"] = summary.ClosedInvoices,
            ["markedOverdue"] = summary.MarkedOverdue
        });
    }
}
