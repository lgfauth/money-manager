using Microsoft.Extensions.Logging;

namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Implementação placeholder para manter o host funcional.
/// Substituir pela implementação real que consome a base (MongoDB) e cria/atualiza transações.
/// </summary>
internal sealed class NoOpTransactionScheduleProcessor(ILogger<NoOpTransactionScheduleProcessor> logger) : ITransactionScheduleProcessor
{
    public Task ProcessAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Nenhum processador configurado ainda. Worker em modo No-Op.");
        return Task.CompletedTask;
    }
}
