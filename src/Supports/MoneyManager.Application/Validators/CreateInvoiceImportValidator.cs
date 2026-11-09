using FluentValidation;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Validators;

/// <summary>
/// Regras do upload de fatura: um PDF (até 20 MB), de 1 a 10 imagens JPEG/PNG/WebP (até 10 MB cada
/// na entrada e 40 MB no total; depois normalizadas para JPEG ≤ 5 MB) ou uma planilha <c>.xlsx</c>
/// (até 5 MB), sem misturar tipos. O conteúdo precisa ter a assinatura do tipo declarado; a planilha
/// ainda passa pelas regras de conteúdo do leitor de <c>.xlsx</c> no service.
/// </summary>
public class CreateInvoiceImportValidator : AbstractValidator<CreateInvoiceImportRequestDto>
{
    public const string PdfContentType = "application/pdf";
    public const long MaxPdfBytes = 20L * 1024 * 1024;
    public const int MaxImages = 10;
    public const long MaxImageBytes = 10L * 1024 * 1024;
    public const long MaxTotalImageBytes = 40L * 1024 * 1024;
    public const long MaxSpreadsheetBytes = 5L * 1024 * 1024;

    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string GenericContentType = "application/octet-stream";

    // Formatos de planilha recusados com orientação para exportar em .xlsx
    private static readonly HashSet<string> LegacySpreadsheetContentTypes = new(StringComparer.Ordinal)
    {
        "application/vnd.ms-excel",
        "application/vnd.ms-excel.sheet.macroenabled.12",
        "application/vnd.ms-excel.sheet.binary.macroenabled.12",
        "text/csv",
        "application/csv"
    };
    private static readonly string[] LegacySpreadsheetExtensions = [".xls", ".xlsm", ".xlsb", ".csv"];

    /// <summary>Limite do corpo da requisição multipart (maior lote permitido + margem do multipart).</summary>
    public const long MaxRequestBytes = 42L * 1024 * 1024;

    public static readonly IReadOnlySet<string> ImageContentTypes =
        new HashSet<string>(StringComparer.Ordinal) { "image/jpeg", "image/png", "image/webp" };

    public const string UnsupportedTypeMessage =
        "Tipo de arquivo não suportado. Envie a fatura em PDF, em imagens JPEG, PNG ou WebP, ou em planilha .xlsx.";
    public const string ExportAsXlsxMessage =
        "Envie a planilha no formato .xlsx. Arquivos .xls, .xlsm, .csv ou planilhas protegidas por senha não são aceitos: abra o arquivo no Excel e salve como .xlsx, sem senha.";

    public CreateInvoiceImportValidator()
    {
        RuleFor(x => x.CreditCardId)
            .NotEmpty().WithMessage("Informe o cartão da fatura.")
            .Matches("^[0-9a-fA-F]{24}$").WithMessage("Cartão inválido.");

        RuleFor(x => x.Files)
            .Must(f => f is { Count: > 0 }).WithMessage("Envie a fatura em PDF ou em imagens.");

        RuleForEach(x => x.Files)
            .Must(f => f.SizeBytes > 0).WithMessage("Um dos arquivos enviados está vazio.")
            .Must(f => !IsLegacySpreadsheet(f)).WithMessage(ExportAsXlsxMessage)
            .Must(f => IsLegacySpreadsheet(f) || ResolveSourceType(f) is not null).WithMessage(UnsupportedTypeMessage);

        When(x => x.Files is { Count: > 0 } && AllSupported(x.Files), () =>
        {
            RuleFor(x => x.Files)
                .Must(f => f.Select(ResolveSourceType).Distinct().Count() == 1)
                .WithMessage("Envie um único PDF, uma única planilha ou só imagens, sem misturar os tipos de arquivo.");

            When(x => IsSingleType(x.Files, InvoiceImportSourceType.Spreadsheet), () =>
            {
                RuleFor(x => x.Files)
                    .Must(f => f.Count == 1).WithMessage("Envie apenas uma planilha por importação.")
                    .Must(f => f.All(file => file.SizeBytes <= MaxSpreadsheetBytes))
                    .WithMessage("A planilha excede o tamanho máximo de 5 MB.")
                    // .xls antigo e .xlsx com senha são arquivos OLE, não ZIP
                    .Must(f => f.All(file => file.SizeBytes == 0 || !StartsWith(file.Content, 0, [0xD0, 0xCF, 0x11, 0xE0])))
                    .WithMessage(ExportAsXlsxMessage)
                    .Must(f => f.All(file => file.SizeBytes == 0 || StartsWith(file.Content, 0, [0xD0, 0xCF, 0x11, 0xE0]) || StartsWith(file.Content, 0, [0x50, 0x4B, 0x03, 0x04])))
                    .WithMessage("O arquivo enviado não é uma planilha .xlsx válida. Exporte a fatura novamente em .xlsx.");
            });

            When(x => IsSingleType(x.Files, InvoiceImportSourceType.Pdf), () =>
            {
                RuleFor(x => x.Files)
                    .Must(f => f.Count == 1).WithMessage("Envie apenas um PDF por importação.")
                    .Must(f => f.All(file => file.SizeBytes <= MaxPdfBytes))
                    .WithMessage("O PDF excede o tamanho máximo de 20 MB.")
                    .Must(f => f.All(file => file.SizeBytes == 0 || HasExpectedSignature(file)))
                    .WithMessage("O arquivo enviado não é um PDF válido.");
            });

            When(x => IsSingleType(x.Files, InvoiceImportSourceType.Images), () =>
            {
                RuleFor(x => x.Files)
                    .Must(f => f.Count <= MaxImages).WithMessage("Envie no máximo 10 imagens por importação.")
                    .Must(f => f.All(file => file.SizeBytes <= MaxImageBytes))
                    .WithMessage("Cada imagem pode ter no máximo 10 MB.")
                    .Must(f => f.Sum(file => file.SizeBytes) <= MaxTotalImageBytes)
                    .WithMessage("As imagens somam mais de 40 MB.")
                    .Must(f => f.All(file => file.SizeBytes == 0 || HasExpectedSignature(file)))
                    .WithMessage("Uma das imagens está corrompida ou não corresponde ao tipo informado.");
            });
        });
    }

    /// <summary>Normaliza o content type (minúsculas, sem parâmetros como charset).</summary>
    public static string NormalizeContentType(string? contentType) =>
        (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

    /// <summary>Tipo de origem correspondente ao content type, ou null se não for suportado.</summary>
    public static InvoiceImportSourceType? ResolveSourceType(string? contentType)
    {
        var normalized = NormalizeContentType(contentType);
        if (normalized == PdfContentType)
            return InvoiceImportSourceType.Pdf;
        if (ImageContentTypes.Contains(normalized))
            return InvoiceImportSourceType.Images;
        if (normalized == XlsxContentType)
            return InvoiceImportSourceType.Spreadsheet;
        return null;
    }

    /// <summary>
    /// Tipo de origem do arquivo: pelo content type ou, para <c>application/octet-stream</c> (enviado por alguns
    /// navegadores e celulares), pela extensão <c>.xlsx</c>. A decisão final é sempre pela assinatura e pelo conteúdo.
    /// </summary>
    public static InvoiceImportSourceType? ResolveSourceType(InvoiceImportUploadFileRequestDto file)
    {
        var byType = ResolveSourceType(file.ContentType);
        if (byType is not null)
            return byType;

        // Alguns sistemas mandam .xlsx como octet-stream ou com o tipo do Excel antigo
        var contentType = NormalizeContentType(file.ContentType);
        return HasXlsxExtension(file) && (contentType == GenericContentType || contentType == "application/vnd.ms-excel")
            ? InvoiceImportSourceType.Spreadsheet
            : null;
    }

    private static bool HasXlsxExtension(InvoiceImportUploadFileRequestDto file) =>
        file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    private static bool IsLegacySpreadsheet(InvoiceImportUploadFileRequestDto file) =>
        !HasXlsxExtension(file)
        && (LegacySpreadsheetContentTypes.Contains(NormalizeContentType(file.ContentType))
            || LegacySpreadsheetExtensions.Any(ext => file.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));

    private static bool AllSupported(List<InvoiceImportUploadFileRequestDto> files) =>
        files.All(f => !IsLegacySpreadsheet(f) && ResolveSourceType(f) is not null);

    private static bool IsSingleType(List<InvoiceImportUploadFileRequestDto> files, InvoiceImportSourceType type) =>
        files is { Count: > 0 } && files.All(f => ResolveSourceType(f) == type);

    // Confere os bytes iniciais do arquivo com o tipo declarado (evita enviar à IA um arquivo com tipo falso)
    private static bool HasExpectedSignature(InvoiceImportUploadFileRequestDto file)
    {
        var c = file.Content;
        return NormalizeContentType(file.ContentType) switch
        {
            PdfContentType => StartsWith(c, 0, "%PDF-"u8),
            "image/jpeg" => StartsWith(c, 0, [0xFF, 0xD8, 0xFF]),
            "image/png" => StartsWith(c, 0, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            "image/webp" => StartsWith(c, 0, "RIFF"u8) && StartsWith(c, 8, "WEBP"u8),
            _ => false
        };
    }

    private static bool StartsWith(byte[] content, int offset, ReadOnlySpan<byte> signature) =>
        content.Length >= offset + signature.Length
        && content.AsSpan(offset, signature.Length).SequenceEqual(signature);
}
