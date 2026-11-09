namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// A biblioteca de processamento de imagens (nativa) não pôde ser carregada no servidor.
/// Falha de ambiente, não do arquivo enviado: responde 503.
/// </summary>
public class ImageProcessingUnavailableException : Exception
{
    public ImageProcessingUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
