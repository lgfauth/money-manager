using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Enums;
using Xunit;

namespace MoneyManager.Tests.Application.Validators;

public class CreateInvoiceImportValidatorTests
{
    private const string CardId = "64b7f0c2a1b2c3d4e5f60718";

    private readonly CreateInvoiceImportValidator _validator = new();

    private static byte[] WithSignature(byte[] signature, long size)
    {
        var content = new byte[size];
        signature.AsSpan(0, (int)Math.Min(signature.Length, size)).CopyTo(content);
        return content;
    }

    internal static InvoiceImportUploadFileRequestDto Pdf(long size = 1024) => new()
    {
        FileName = "fatura.pdf",
        ContentType = "application/pdf",
        Content = WithSignature("%PDF-1.7"u8.ToArray(), size)
    };

    internal static InvoiceImportUploadFileRequestDto Jpeg(long size = 1024) => new()
    {
        FileName = "print.jpg",
        ContentType = "image/jpeg",
        Content = WithSignature([0xFF, 0xD8, 0xFF, 0xE0], size)
    };

    private static InvoiceImportUploadFileRequestDto Png(long size = 1024) => new()
    {
        FileName = "print.png",
        ContentType = "image/png",
        Content = WithSignature([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], size)
    };

    private static InvoiceImportUploadFileRequestDto Webp(long size = 1024)
    {
        var content = WithSignature("RIFF"u8.ToArray(), size);
        "WEBP"u8.ToArray().CopyTo(content, 8);
        return new() { FileName = "print.webp", ContentType = "image/webp", Content = content };
    }

    private static CreateInvoiceImportRequestDto Request(params InvoiceImportUploadFileRequestDto[] files) =>
        new() { CreditCardId = CardId, Files = [.. files] };

    private async Task<string[]> ErrorsOf(CreateInvoiceImportRequestDto request)
    {
        var result = await _validator.ValidateAsync(request);
        return result.Errors.Select(e => e.ErrorMessage).ToArray();
    }

    [Fact]
    public async Task Validate_WithSinglePdf_ShouldPass()
    {
        // Act
        var result = await _validator.ValidateAsync(Request(Pdf()));

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithTenImagesOfEachSupportedType_ShouldPass()
    {
        // Arrange
        var request = Request(
            Jpeg(), Png(), Webp(), Jpeg(), Png(), Webp(), Jpeg(), Png(), Webp(),
            Jpeg(CreateInvoiceImportValidator.MaxImageBytes));

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithContentTypeParametersAndUppercase_ShouldPass()
    {
        // Arrange
        var file = Pdf();
        file.ContentType = "Application/PDF; charset=binary";

        // Act
        var result = await _validator.ValidateAsync(Request(file));

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("nao-e-um-id")]
    public async Task Validate_WithInvalidCardId_ShouldFail(string cardId)
    {
        // Arrange
        var request = Request(Pdf());
        request.CreditCardId = cardId;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateInvoiceImportRequestDto.CreditCardId));
    }

    [Fact]
    public async Task Validate_WithoutFiles_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request());

        // Assert
        Assert.Contains("Envie a fatura em PDF ou em imagens.", errors);
    }

    [Fact]
    public async Task Validate_WithEmptyFile_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Pdf(size: 0)));

        // Assert
        Assert.Contains("Um dos arquivos enviados está vazio.", errors);
    }

    [Theory]
        [InlineData("image/gif")]
    [InlineData("")]
    public async Task Validate_WithUnsupportedType_ShouldFail(string contentType)
    {
        // Arrange
        var file = Pdf();
        file.ContentType = contentType;

        // Act
        var errors = await ErrorsOf(Request(file));

        // Assert
        Assert.Contains(CreateInvoiceImportValidator.UnsupportedTypeMessage, errors);
    }

    [Fact]
    public async Task Validate_MixingPdfAndImages_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Pdf(), Jpeg()));

        // Assert
        Assert.Contains("Envie um único PDF, uma única planilha ou só imagens, sem misturar os tipos de arquivo.", errors);
    }

    [Fact]
    public async Task Validate_WithTwoPdfs_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Pdf(), Pdf()));

        // Assert
        Assert.Contains("Envie apenas um PDF por importação.", errors);
    }

    [Fact]
    public async Task Validate_WithPdfOverLimit_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Pdf(CreateInvoiceImportValidator.MaxPdfBytes + 1)));

        // Assert
        Assert.Contains("O PDF excede o tamanho máximo de 20 MB.", errors);
    }

    [Fact]
    public async Task Validate_WithElevenImages_ShouldFail()
    {
        // Arrange
        var request = Request(Enumerable.Range(0, 11).Select(_ => Jpeg()).ToArray());

        // Act
        var errors = await ErrorsOf(request);

        // Assert
        Assert.Contains("Envie no máximo 10 imagens por importação.", errors);
    }

    [Fact]
    public async Task Validate_WithImageOverTenMegabytes_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Jpeg(CreateInvoiceImportValidator.MaxImageBytes + 1)));

        // Assert
        Assert.Contains("Cada imagem pode ter no máximo 10 MB.", errors);
    }

    [Fact]
    public async Task Validate_WithPdfWithoutPdfSignature_ShouldFail()
    {
        // Arrange
        var file = Pdf();
        file.Content = WithSignature([0xFF, 0xD8, 0xFF], 1024);

        // Act
        var errors = await ErrorsOf(Request(file));

        // Assert
        Assert.Contains("O arquivo enviado não é um PDF válido.", errors);
    }

    [Fact]
    public async Task Validate_WithPngDeclaredAsJpeg_ShouldFail()
    {
        // Arrange
        var file = Png();
        file.ContentType = "image/jpeg";

        // Act
        var errors = await ErrorsOf(Request(file));

        // Assert
        Assert.Contains("Uma das imagens está corrompida ou não corresponde ao tipo informado.", errors);
    }

    [Theory]
    [InlineData("application/pdf", InvoiceImportSourceType.Pdf)]
    [InlineData("IMAGE/PNG", InvoiceImportSourceType.Images)]
    [InlineData("image/webp", InvoiceImportSourceType.Images)]
    [InlineData("text/plain", null)]
    [InlineData(null, null)]
    public void ResolveSourceType_ShouldMapContentType(string? contentType, InvoiceImportSourceType? expected)
    {
        // Act & Assert
        Assert.Equal(expected, CreateInvoiceImportValidator.ResolveSourceType(contentType));
    }

    // --- Planilha .xlsx ---

    private static InvoiceImportUploadFileRequestDto Xlsx(
        long size = 1024,
        string contentType = CreateInvoiceImportValidator.XlsxContentType,
        string fileName = "fatura.xlsx") => new()
    {
        FileName = fileName,
        ContentType = contentType,
        Content = WithSignature([0x50, 0x4B, 0x03, 0x04], size)
    };

    [Theory]
    [InlineData(CreateInvoiceImportValidator.XlsxContentType, "fatura.xlsx")]
    [InlineData("application/octet-stream", "fatura.XLSX")]
    [InlineData("application/vnd.ms-excel", "fatura.xlsx")]
    public async Task Validate_WithXlsx_ShouldPass(string contentType, string fileName)
    {
        // Act
        var result = await _validator.ValidateAsync(Request(Xlsx(contentType: contentType, fileName: fileName)));

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(InvoiceImportSourceType.Spreadsheet, CreateInvoiceImportValidator.ResolveSourceType(Xlsx(contentType: contentType, fileName: fileName)));
    }

    [Fact]
    public async Task Validate_WithOctetStreamWithoutXlsxExtension_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Xlsx(contentType: "application/octet-stream", fileName: "fatura.bin")));

        // Assert
        Assert.Contains(CreateInvoiceImportValidator.UnsupportedTypeMessage, errors);
    }

    [Theory]
    [InlineData("application/vnd.ms-excel", "fatura.xls")]
    [InlineData("application/vnd.ms-excel.sheet.macroEnabled.12", "fatura.xlsm")]
    [InlineData("text/csv", "fatura.csv")]
    [InlineData("application/octet-stream", "fatura.xls")]
    public async Task Validate_WithLegacySpreadsheetFormats_ShouldAskToExportAsXlsx(string contentType, string fileName)
    {
        // Act
        var errors = await ErrorsOf(Request(Xlsx(contentType: contentType, fileName: fileName)));

        // Assert
        Assert.Contains(CreateInvoiceImportValidator.ExportAsXlsxMessage, errors);
    }

    [Fact]
    public async Task Validate_WithPasswordProtectedXlsx_ShouldAskToExportAsXlsx()
    {
        // Arrange: .xlsx com senha é um arquivo OLE/CFB
        var file = Xlsx();
        file.Content = WithSignature([0xD0, 0xCF, 0x11, 0xE0], 1024);

        // Act
        var errors = await ErrorsOf(Request(file));

        // Assert
        Assert.Contains(CreateInvoiceImportValidator.ExportAsXlsxMessage, errors);
    }

    [Fact]
    public async Task Validate_WithXlsxExtensionButNotZip_ShouldFail()
    {
        // Arrange: CSV renomeado para .xlsx
        var file = Xlsx();
        file.Content = "Data;Valor"u8.ToArray();

        // Act
        var errors = await ErrorsOf(Request(file));

        // Assert
        Assert.Contains("O arquivo enviado não é uma planilha .xlsx válida. Exporte a fatura novamente em .xlsx.", errors);
    }

    [Fact]
    public async Task Validate_WithSpreadsheetOverFiveMegabytes_ShouldFail()
    {
        // Act
        var errors = await ErrorsOf(Request(Xlsx(CreateInvoiceImportValidator.MaxSpreadsheetBytes + 1)));

        // Assert
        Assert.Contains("A planilha excede o tamanho máximo de 5 MB.", errors);
    }

    [Fact]
    public async Task Validate_WithTwoSpreadsheetsOrMixedWithPdf_ShouldFail()
    {
        // Act
        var two = await ErrorsOf(Request(Xlsx(), Xlsx()));
        var mixed = await ErrorsOf(Request(Xlsx(), Pdf()));

        // Assert
        Assert.Contains("Envie apenas uma planilha por importação.", two);
        Assert.Contains("Envie um único PDF, uma única planilha ou só imagens, sem misturar os tipos de arquivo.", mixed);
    }
}
