using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

/// <summary>
/// Repositório para gerenciar faturas de cartão de crédito
/// </summary>
public interface ICreditCardInvoiceRepository : IRepository<CreditCardInvoice>
{
    /// <summary>
    /// Busca a fatura aberta (Open) de um cartão específico
    /// </summary>
    Task<CreditCardInvoice?> GetOpenInvoiceByAccountIdAsync(string accountId);

    /// <summary>
    /// Busca todas as faturas de um cartão específico
    /// </summary>
    Task<IEnumerable<CreditCardInvoice>> GetByAccountIdAsync(string accountId);

    /// <summary>
    /// Busca faturas fechadas (Closed) que ainda não foram pagas
    /// </summary>
    Task<IEnumerable<CreditCardInvoice>> GetClosedUnpaidInvoicesAsync(string userId);

    /// <summary>
    /// Busca faturas vencidas (Overdue) de um usuário
    /// </summary>
    Task<IEnumerable<CreditCardInvoice>> GetOverdueInvoicesAsync(string userId);

    /// <summary>
    /// Busca fatura por referência de mês (ex: "2026-02")
    /// </summary>
    Task<CreditCardInvoice?> GetByReferenceMonthAsync(string accountId, string referenceMonth);

    /// <summary>
    /// Busca faturas dentro de um período de datas
    /// </summary>
    Task<IEnumerable<CreditCardInvoice>> GetByPeriodAsync(string accountId, DateTime start, DateTime end);
}
