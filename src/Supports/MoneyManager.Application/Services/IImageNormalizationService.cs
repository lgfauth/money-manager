namespace MoneyManager.Application.Services;

/// <summary>
/// Normaliza imagens enviadas para a IA: corrige a orientação EXIF, reduz para o lado maior
/// configurado e recomprime em JPEG dentro do tamanho máximo. Toda imagem sai em JPEG.
/// </summary>
public interface IImageNormalizationService
{
    /// <summary>
    /// Devolve a imagem normalizada. Lança <see cref="MoneyManager.Domain.Exceptions.ImageNormalizationException"/>
    /// (400) para imagem corrompida, com tipo falso, grande demais ou que não coube no limite, e
    /// <see cref="MoneyManager.Domain.Exceptions.ImageProcessingUnavailableException"/> (503) se a biblioteca
    /// de imagens não puder ser carregada no servidor.
    /// </summary>
    NormalizedImage Normalize(byte[] content);
}

public sealed record NormalizedImage(byte[] Content, string ContentType, int Width, int Height);
