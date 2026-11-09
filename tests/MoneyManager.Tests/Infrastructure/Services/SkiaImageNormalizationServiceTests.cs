using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Infrastructure.Services;
using NSubstitute;
using SkiaSharp;
using Xunit;

namespace MoneyManager.Tests.Infrastructure.Services;

/// <summary>Imagens geradas no próprio teste (nenhum arquivo real de fatura).</summary>
public class SkiaImageNormalizationServiceTests
{
    private static SkiaImageNormalizationService CreateService(ImageNormalizationOptions? options = null) =>
        new(Options.Create(options ?? new ImageNormalizationOptions()),
            Substitute.For<ILogger<SkiaImageNormalizationService>>());

    /// <summary>Imagem com a metade esquerda vermelha e a direita azul.</summary>
    private static byte[] TwoColorImage(int width, int height, SKEncodedImageFormat format, int quality = 90)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            using var red = new SKPaint { Color = SKColors.Red };
            using var blue = new SKPaint { Color = SKColors.Blue };
            canvas.DrawRect(0, 0, width / 2f, height, red);
            canvas.DrawRect(width / 2f, 0, width / 2f, height, blue);
        }
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, quality);
        return data.ToArray();
    }

    /// <summary>Insere um segmento EXIF (APP1) com a tag Orientation logo após o SOI do JPEG.</summary>
    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,                         // APP1, tamanho 34
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,             // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF little-endian, IFD em 8
            0x01, 0x00,                                     // 1 entrada
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, // Orientation, SHORT, 1 valor
            (byte)orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00                          // sem próximo IFD
        ];
        return [.. jpeg.Take(2), .. app1, .. jpeg.Skip(2)];
    }

    private static SKBitmap DecodeOutput(byte[] content)
    {
        using var codec = SKCodec.Create(SKData.CreateCopy(content));
        Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        return SKBitmap.Decode(content);
    }

    private static bool IsReddish(SKColor c) => c.Red > 180 && c.Blue < 90;
    private static bool IsBluish(SKColor c) => c.Blue > 180 && c.Red < 90;

    [Fact]
    public void Normalize_LargePng_ShouldReduceLongSideAndKeepAspectRatioAsJpeg()
    {
        // Arrange
        var service = CreateService();
        var png = TwoColorImage(4000, 3000, SKEncodedImageFormat.Png);

        // Act
        var result = service.Normalize(png);

        // Assert
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal((1568, 1176), (result.Width, result.Height));
        Assert.True(result.Content.Length <= 5 * 1024 * 1024);
        using var output = DecodeOutput(result.Content);
        Assert.Equal((1568, 1176), (output.Width, output.Height));
        Assert.True(IsReddish(output.GetPixel(100, 500)));
        Assert.True(IsBluish(output.GetPixel(1400, 500)));
    }

    [Fact]
    public void Normalize_LargeJpeg_ShouldUseScaledDecodeAndReachTargetSize()
    {
        // Arrange
        var service = CreateService();
        var jpeg = TwoColorImage(4032, 3024, SKEncodedImageFormat.Jpeg);

        // Act
        var result = service.Normalize(jpeg);

        // Assert
        Assert.Equal((1568, 1176), (result.Width, result.Height));
    }

    [Fact]
    public void Normalize_SmallImage_ShouldKeepSizeAndStillConvertToJpeg()
    {
        // Arrange
        var service = CreateService();
        var webp = TwoColorImage(300, 200, SKEncodedImageFormat.Webp);

        // Act
        var result = service.Normalize(webp);

        // Assert
        Assert.Equal("image/jpeg", result.ContentType);
        Assert.Equal((300, 200), (result.Width, result.Height));
        using var output = DecodeOutput(result.Content);
        Assert.Equal((300, 200), (output.Width, output.Height));
    }

    [Fact]
    public void Normalize_PhotoWithExifRotation_ShouldApplyOrientation()
    {
        // Arrange: armazenada 200×100 (vermelho à esquerda), EXIF 6 = girar 90° no sentido horário
        var service = CreateService();
        var jpeg = WithExifOrientation(TwoColorImage(200, 100, SKEncodedImageFormat.Jpeg), 6);

        // Act
        var result = service.Normalize(jpeg);

        // Assert: fica em pé (100×200), com a metade vermelha em cima
        Assert.Equal((100, 200), (result.Width, result.Height));
        using var output = DecodeOutput(result.Content);
        Assert.True(IsReddish(output.GetPixel(50, 30)));
        Assert.True(IsBluish(output.GetPixel(50, 170)));
    }

    [Fact]
    public void Normalize_PhotoWithExif180_ShouldFlipBothAxes()
    {
        // Arrange: EXIF 3 = 180°
        var service = CreateService();
        var jpeg = WithExifOrientation(TwoColorImage(200, 100, SKEncodedImageFormat.Jpeg), 3);

        // Act
        var result = service.Normalize(jpeg);

        // Assert: azul passa para a esquerda
        Assert.Equal((200, 100), (result.Width, result.Height));
        using var output = DecodeOutput(result.Content);
        Assert.True(IsBluish(output.GetPixel(30, 50)));
        Assert.True(IsReddish(output.GetPixel(170, 50)));
    }

    [Fact]
    public void Normalize_TransparentPng_ShouldUseWhiteBackground()
    {
        // Arrange
        var service = CreateService();
        using var bitmap = new SKBitmap(new SKImageInfo(50, 50, SKColorType.Rgba8888, SKAlphaType.Premul));
        bitmap.Erase(SKColors.Transparent);
        using var image = SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);

        // Act
        var result = service.Normalize(png.ToArray());

        // Assert
        using var output = DecodeOutput(result.Content);
        var pixel = output.GetPixel(25, 25);
        Assert.True(pixel.Red > 240 && pixel.Green > 240 && pixel.Blue > 240);
    }

    [Fact]
    public void Normalize_WithRandomBytes_ShouldThrowCorrupt()
    {
        // Arrange
        var service = CreateService();
        var garbage = new byte[2048];
        new Random(42).NextBytes(garbage);

        // Act & Assert
        var ex = Assert.Throws<ImageNormalizationException>(() => service.Normalize(garbage));
        Assert.Equal(SkiaImageNormalizationService.CorruptImageMessage, ex.Message);
    }

    [Fact]
    public void Normalize_WithJpegSignatureButBrokenBody_ShouldThrowCorrupt()
    {
        // Arrange: assinatura JPEG válida (passa no validator), conteúdo inválido
        var service = CreateService();
        byte[] fake = [0xFF, 0xD8, 0xFF, 0xE0, .. "conteudo que nao e imagem"u8.ToArray()];

        // Act & Assert
        Assert.Throws<ImageNormalizationException>(() => service.Normalize(fake));
    }

    [Fact]
    public void Normalize_WithTruncatedJpeg_ShouldThrowCorrupt()
    {
        // Arrange
        var service = CreateService();
        var jpeg = TwoColorImage(800, 600, SKEncodedImageFormat.Jpeg);
        var truncated = jpeg.Take(jpeg.Length / 2).ToArray();

        // Act & Assert
        Assert.Throws<ImageNormalizationException>(() => service.Normalize(truncated));
    }

    [Fact]
    public void Normalize_WithEmptyContent_ShouldThrowCorrupt()
    {
        // Act & Assert
        Assert.Throws<ImageNormalizationException>(() => CreateService().Normalize([]));
    }

    [Fact]
    public void Normalize_AboveMaxInputPixels_ShouldThrowBeforeDecoding()
    {
        // Arrange
        var service = CreateService(new ImageNormalizationOptions { MaxInputPixels = 1_000 });

        // Act & Assert
        var ex = Assert.Throws<ImageNormalizationException>(() =>
            service.Normalize(TwoColorImage(100, 100, SKEncodedImageFormat.Png)));
        Assert.Equal(SkiaImageNormalizationService.TooManyPixelsMessage, ex.Message);
    }

    [Fact]
    public void Normalize_WhenNoQualityFitsMaxOutput_ShouldThrowTooLarge()
    {
        // Arrange
        var service = CreateService(new ImageNormalizationOptions { MaxOutputBytes = 100 });

        // Act & Assert
        var ex = Assert.Throws<ImageNormalizationException>(() =>
            service.Normalize(TwoColorImage(400, 300, SKEncodedImageFormat.Png)));
        Assert.Equal(SkiaImageNormalizationService.TooLargeMessage, ex.Message);
    }

    [Fact]
    public void Normalize_ShouldLowerQualityUntilItFits()
    {
        // Arrange: ruído não comprime bem; o limite só é atingido com qualidade menor que a inicial
        var noisy = new SKBitmap(600, 600);
        var random = new Random(7);
        for (var x = 0; x < 600; x++)
            for (var y = 0; y < 600; y++)
                noisy.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
        using var image = SKImage.FromBitmap(noisy);
        var png = image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
        var atInitial = CreateService().Normalize(png).Content.Length;
        var service = CreateService(new ImageNormalizationOptions { MaxOutputBytes = atInitial - 1 });

        // Act
        var result = service.Normalize(png);

        // Assert
        Assert.True(result.Content.Length < atInitial);
        noisy.Dispose();
    }
}
