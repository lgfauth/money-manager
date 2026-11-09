namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// Falha na análise de uma fatura pela IA. <see cref="IsTransient"/> indica falha temporária
/// (rede, 5xx, 429, serviço sobrecarregado) que pode ser tentada de novo; caso contrário a falha
/// é definitiva (chave ausente, recusa, resposta interrompida, JSON inválido).
/// A mensagem é amigável, em português, e pode ser exibida ao usuário.
/// </summary>
public class InvoiceDocumentAnalysisException : Exception
{
    public bool IsTransient { get; }

    public InvoiceDocumentAnalysisException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}
