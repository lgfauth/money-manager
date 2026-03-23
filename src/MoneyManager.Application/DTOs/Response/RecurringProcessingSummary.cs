namespace MoneyManager.Application.DTOs.Response;

/// <summary>
/// Resultado do processamento em lote de recorrências.
/// Carrega o número de transações criadas por userId para que o Worker
/// possa enviar push notifications sem precisar re-consultar o banco.
/// </summary>
public sealed class RecurringProcessingSummary
{
    /// <summary>userId ? quantidade de transações criadas nesta execução.</summary>
    public Dictionary<string, int> ProcessedByUser { get; } = [];

    public int TotalProcessed => ProcessedByUser.Values.Sum();

    internal void Add(string userId, int count)
    {
        if (ProcessedByUser.TryGetValue(userId, out var existing))
            ProcessedByUser[userId] = existing + count;
        else
            ProcessedByUser[userId] = count;
    }
}
