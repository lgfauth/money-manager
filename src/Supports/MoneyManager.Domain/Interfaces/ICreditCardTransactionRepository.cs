using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardTransactionRepository : IRepository<CreditCardTransaction>
{
    Task<IEnumerable<CreditCardTransaction>> GetByUserAsync(string userId);
    Task<IEnumerable<CreditCardTransaction>> GetByInvoiceAsync(string userId, string invoiceId);
    Task<IEnumerable<CreditCardTransaction>> GetByCardAsync(string userId, string creditCardId);
    Task<IEnumerable<CreditCardTransaction>> GetByParentAsync(string userId, string parentTransactionId);
}
