using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardInvoiceImportRepository : IRepository<CreditCardInvoiceImport>
{
    /// <summary>Importação do usuário pelo id (não excluída); null se não existir ou for de outro usuário.</summary>
    Task<CreditCardInvoiceImport?> GetByIdForUserAsync(string userId, string importId);

    /// <summary>Importações mais recentes do usuário (não excluídas), da mais nova para a mais antiga.</summary>
    Task<IEnumerable<CreditCardInvoiceImport>> GetRecentByUserAsync(string userId, int limit);

    /// <summary>Job ativo (Queued ou Processing) do usuário para o cartão, se houver.</summary>
    Task<CreditCardInvoiceImport?> GetActiveByCardAsync(string userId, string creditCardId);

    /// <summary>
    /// Última importação confirmada do cartão; com <paramref name="referenceMonth"/>, só a desse mês de referência.
    /// </summary>
    Task<CreditCardInvoiceImport?> GetLastConfirmedByCardAsync(string userId, string creditCardId, string? referenceMonth = null);

    /// <summary>
    /// Reserva atomicamente o job Queued mais antigo, mudando-o para Processing.
    /// Dois workers nunca recebem o mesmo job.
    /// </summary>
    Task<CreditCardInvoiceImport?> ClaimNextQueuedAsync(DateTime nowUtc);

    /// <summary>Indica se há algum job Queued (não excluído) aguardando o Worker.</summary>
    Task<bool> AnyQueuedAsync();

    /// <summary>Jobs em Processing iniciados antes de <paramref name="startedBeforeUtc"/> (presos).</summary>
    Task<IEnumerable<CreditCardInvoiceImport>> GetStuckProcessingAsync(DateTime startedBeforeUtc);

    /// <summary>Jobs Queued criados antes de <paramref name="createdBeforeUtc"/> (antigos demais para processar).</summary>
    Task<IEnumerable<CreditCardInvoiceImport>> GetQueuedCreatedBeforeAsync(DateTime createdBeforeUtc);
}
