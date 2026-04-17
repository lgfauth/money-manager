using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardInvoiceRepository : IRepository<CreditCardInvoice>
{
    Task<IEnumerable<CreditCardInvoice>> GetByUserAsync(string userId);
    Task<IEnumerable<CreditCardInvoice>> GetByCardAsync(string userId, string creditCardId);
    Task<CreditCardInvoice?> GetByCardAndReferenceAsync(string userId, string creditCardId, string referenceMonth);
    Task<IEnumerable<CreditCardInvoice>> GetByStatusAsync(InvoiceStatus status);
}
