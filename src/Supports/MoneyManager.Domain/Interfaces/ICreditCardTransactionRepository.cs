using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardTransactionRepository : IRepository<CreditCardTransaction>
{
    Task<IEnumerable<CreditCardTransaction>> GetByUserAsync(string userId);
    Task<IEnumerable<CreditCardTransaction>> GetByInvoiceAsync(string userId, string invoiceId);
    Task<IEnumerable<CreditCardTransaction>> GetByCardAsync(string userId, string creditCardId);
    Task<IEnumerable<CreditCardTransaction>> GetByParentAsync(string userId, string parentTransactionId);
    Task<CreditCardTransaction?> GetByExternalIdAsync(string userId, string externalId);
    Task<IEnumerable<CreditCardTransaction>> GetByUserFromPurchaseDateAsync(string userId, DateTime fromDate, DateTime installmentsFromDate);
    Task<IEnumerable<CreditCardTransaction>> GetByInvoicesAsync(string userId, IReadOnlyCollection<string> invoiceIds);
}
