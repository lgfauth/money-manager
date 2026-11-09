using MoneyManager.Domain.Entities;

namespace MoneyManager.Domain.Interfaces;

public interface ICreditCardInvoiceImportFileRepository : IRepository<CreditCardInvoiceImportFile>
{
    Task AddManyAsync(IEnumerable<CreditCardInvoiceImportFile> files);

    /// <summary>Arquivos da importação, na ordem do upload.</summary>
    Task<IEnumerable<CreditCardInvoiceImportFile>> GetByImportAsync(string userId, string importId);

    Task<long> CountByImportAsync(string userId, string importId);

    /// <summary>Remove fisicamente os arquivos da importação.</summary>
    Task DeleteByImportAsync(string userId, string importId);
}
