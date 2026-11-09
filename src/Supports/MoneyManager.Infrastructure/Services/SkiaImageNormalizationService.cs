using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Exceptions;
using SkiaSharp;

namespace MoneyManager.Infrastructure.Services;

/// <summary>Opções da normalização de imagens (seção <c>ImageNormalization</c>).</summary>
public class ImageNormalizationOptions
{
    public const string SectionName = "ImageNormalization";

    /// <summary>Lado maior máximo, em pixels; acima disso a própria Anthropic reduz a imagem.</summary>
    public int MaxLongSide { get; set; } = 1568;

    public int InitialQuality { get; set; } = 85;
    public int MinQuality { get; set; } = 40;
    public int QualityStep { get; set; } = 10;

    /// <summary>Tamanho máximo do JPEG gerado (limite da Anthropic por imagem).</summary>
    public long MaxOutputBytes { get; set; } = 5L * 1024 * 1024;

    /// <summary>Resolução máxima aceita na entrada (protege a memória contra imagens enormes).</summary>
    public long MaxInputPixels { get; set; } = 60_000_000;
}

/// <summary>
/// Normalização de imagens com SkiaSharp: decodifica (reduzindo já na decodificação quando o formato
/// permite), aplica a orientação EXIF, redimensiona para o lado maior configurado sobre fundo branco e
/// recomprime em JPEG com qualidade decrescente até caber em <see cref="ImageNormalizationOptions.MaxOutputBytes"/>.
/// </summary>
public class SkiaImageNormalizationService : IImageNormalizationService
{
    public const string CorruptImageMessage =
        "Uma das imagens está corrompida ou não é uma imagem válida. Envie a imagem novamente.";
    public const string TooManyPixelsMessage =
        "Uma das imagens tem resolução alta demais para ser processada. Envie um print ou uma foto menor.";
    public const string TooLargeMessage =
        "Não foi possível reduzir uma das imagens para até 5 MB. Envie um print com menos detalhes ou a fatura em PDF.";
    public const string UnavailableMessage =
        "O processamento de imagens está indisponível no momento. Tente novamente mais tarde ou envie a fatura em PDF.";

    private const string JpegContentType = "image/jpeg";

    private readonly ImageNormalizationOptions _options;
    private readonly ILogger<SkiaImageNormalizationService> _logger;

    public SkiaImageNormalizationService(
        IOptions<ImageNormalizationOptions> options,
        ILogger<SkiaImageNormalizationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public NormalizedImage Normalize(byte[] content)
    {
        try
        {
            return NormalizeCore(content);
        }
        catch (Exception ex) when (IsNativeLoadFailure(ex))
        {
            // Erro de ambiente (biblioteca nativa ausente na imagem Docker), não do arquivo
            _logger.LogError(ex,
                "Biblioteca nativa do SkiaSharp (libSkiaSharp) não pôde ser carregada; verifique o pacote SkiaSharp.NativeAssets.Linux.NoDependencies na publicação");
            throw new ImageProcessingUnavailableException(UnavailableMessage, ex);
        }
    }

    private NormalizedImage NormalizeCore(byte[] content)
    {
        if (content.Length == 0)
            throw new ImageNormalizationException(CorruptImageMessage);

        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data)
                          ?? throw new ImageNormalizationException(CorruptImageMessage);

        var sourceInfo = codec.Info;
        if (sourceInfo.Width <= 0 || sourceInfo.Height <= 0)
            throw new ImageNormalizationException(CorruptImageMessage);

        if ((long)sourceInfo.Width * sourceInfo.Height > _options.MaxInputPixels)
            throw new ImageNormalizationException(TooManyPixelsMessage);

        var origin = codec.EncodedOrigin;

        // Dimensões finais (já na orientação correta), com o lado maior limitado
        var swapsAxes = SwapsAxes(origin);
        var orientedWidth = swapsAxes ? sourceInfo.Height : sourceInfo.Width;
        var orientedHeight = swapsAxes ? sourceInfo.Width : sourceInfo.Height;
        var scale = Math.Min(1d, (double)_options.MaxLongSide / Math.Max(orientedWidth, orientedHeight));

        using var decoded = Decode(codec, scale);
        var targetWidth = Math.Max(1, (int)Math.Round(orientedWidth * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(orientedHeight * scale));

        // Tamanho desenhado antes da rotação
        var drawWidth = swapsAxes ? targetHeight : targetWidth;
        var drawHeight = swapsAxes ? targetWidth : targetHeight;

        using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul))
                            ?? throw new ImageNormalizationException(TooManyPixelsMessage);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White); // JPEG não tem transparência
        canvas.SetMatrix(OrientationMatrix(origin, drawWidth, drawHeight));

        using (var source = SKImage.FromBitmap(decoded))
        using (var paint = new SKPaint { IsAntialias = true })
        {
            canvas.DrawImage(
                source,
                SKRect.Create(0, 0, drawWidth, drawHeight),
                new SKSamplingOptions(SKCubicResampler.Mitchell),
                paint);
        }
        canvas.Flush();

        using var result = surface.Snapshot();
        for (var quality = _options.InitialQuality; quality >= _options.MinQuality; quality -= Math.Max(1, _options.QualityStep))
        {
            using var encoded = result.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (encoded is null)
                throw new ImageNormalizationException(CorruptImageMessage);

            if (encoded.Size <= _options.MaxOutputBytes)
                return new NormalizedImage(encoded.ToArray(), JpegContentType, targetWidth, targetHeight);
        }

        throw new ImageNormalizationException(TooLargeMessage);
    }

    /// <summary>
    /// Decodifica a imagem; imagem truncada (<see cref="SKCodecResult.IncompleteInput"/>) é tratada como corrompida.
    /// </summary>
    private static SKBitmap Decode(SKCodec codec, double scale)
    {
        var size = new SKSizeI(codec.Info.Width, codec.Info.Height);
        if (scale < 1)
        {
            // JPEG decodifica direto em 1/2, 1/4 ou 1/8, sem alocar a foto inteira; só usa se não ficar menor que o alvo
            var scaled = codec.GetScaledDimensions((float)scale);
            if (scaled.Width >= Math.Ceiling(codec.Info.Width * scale) && scaled.Height >= Math.Ceiling(codec.Info.Height * scale))
                size = scaled;
        }

        var info = new SKImageInfo(size.Width, size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var bitmap = new SKBitmap(info);
        var result = codec.GetPixels(info, bitmap.GetPixels());
        if (result != SKCodecResult.Success)
        {
            bitmap.Dispose();
            throw new ImageNormalizationException(CorruptImageMessage);
        }
        return bitmap;
    }

    private static bool SwapsAxes(SKEncodedOrigin origin) =>
        origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    /// <summary>
    /// Matriz que leva a imagem armazenada (desenhada em <paramref name="w"/> × <paramref name="h"/>) para a
    /// orientação de exibição indicada pelo EXIF.
    /// </summary>
    internal static SKMatrix OrientationMatrix(SKEncodedOrigin origin, float w, float h) => origin switch
    {
        // Espelhada na horizontal
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        // 180°
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        // Espelhada na vertical
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        // Transposta
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        // 90° no sentido horário
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        // Transversa
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        // 90° no sentido anti-horário
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity
    };

    private static bool IsNativeLoadFailure(Exception ex) => ex switch
    {
        DllNotFoundException or EntryPointNotFoundException or BadImageFormatException => true,
        TypeInitializationException tie => tie.InnerException is not null && IsNativeLoadFailure(tie.InnerException),
        _ => false
    };
}
