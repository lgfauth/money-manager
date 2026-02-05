namespace TransactionSchedulerWorker.WorkerHost.Services;

/// <summary>
/// Porta de entrada do Worker para processar registros temporários (recorrências e parcelamentos).
/// Implementação real será adicionada integrando com Infrastructure/Application.
/// </summary>
public interface ITransactionScheduleProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
