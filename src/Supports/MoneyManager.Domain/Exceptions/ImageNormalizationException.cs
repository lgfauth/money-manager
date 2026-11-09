namespace MoneyManager.Domain.Exceptions;

/// <summary>
/// Imagem enviada que não pôde ser normalizada (corrompida, tipo falso, resolução excessiva ou acima do
/// tamanho máximo mesmo recomprimida). Responde 400 (é uma <see cref="InvalidOperationException"/>).
/// </summary>
public class ImageNormalizationException : InvalidOperationException
{
    public ImageNormalizationException(string message)
        : base(message) { }
}
