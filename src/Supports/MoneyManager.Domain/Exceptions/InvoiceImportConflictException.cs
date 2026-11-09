namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// Operação de importação de fatura recusada pelo estado atual (job ativo no cartão, cartão
/// sincronizado pelo Open Finance ou importação em estado que não permite a ação). Responde 409.
/// </summary>
public class InvoiceImportConflictException : Exception
{
    public InvoiceImportConflictException(string message)
        : base(message) { }
}
