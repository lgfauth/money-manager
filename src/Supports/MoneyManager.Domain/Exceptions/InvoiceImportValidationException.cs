namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// Confirmação de importação recusada por itens ou titulares inválidos; nada é cadastrado.
/// <see cref="Errors"/> traz um texto por problema. Responde 400 (é uma <see cref="InvalidOperationException"/>).
/// </summary>
public class InvoiceImportValidationException : InvalidOperationException
{
    public IReadOnlyList<string> Errors { get; }

    public InvoiceImportValidationException(string message, IReadOnlyList<string>? errors = null)
        : base(message)
    {
        Errors = errors ?? [];
    }
}
