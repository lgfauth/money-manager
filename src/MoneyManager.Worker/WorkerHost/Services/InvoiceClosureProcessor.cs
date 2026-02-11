using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Processador responsável por fechar faturas de cartão de crédito automaticamente
/// Executado diariamente à meia-noite
/// </summary>
internal sealed class InvoiceClosureProcessor(
    ILogger<InvoiceClosureProcessor> logger,
    ICreditCardInvoiceService invoiceService)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogInformation("========================================");
        logger.LogInformation("Iniciando processamento de fechamento de faturas...");
        logger.LogInformation("Data/Hora: {DateTime}", DateTime.UtcNow);
        logger.LogInformation("========================================");

        try
        {
            await invoiceService.ProcessMonthlyInvoiceClosuresAsync();
            logger.LogInformation("Fechamento de faturas processado com sucesso");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar fechamento de faturas");
            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
