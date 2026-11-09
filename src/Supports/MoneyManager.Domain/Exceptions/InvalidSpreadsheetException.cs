namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// Planilha enviada recusada pelas regras de formato ou segurança (não é <c>.xlsx</c>, protegida por senha,
/// com macros, ZIP bomb, limites de conteúdo). Responde 400 (é uma <see cref="InvalidOperationException"/>).
/// </summary>
public class InvalidSpreadsheetException : InvalidOperationException
{
    public InvalidSpreadsheetException(string message)
        : base(message) { }
}
