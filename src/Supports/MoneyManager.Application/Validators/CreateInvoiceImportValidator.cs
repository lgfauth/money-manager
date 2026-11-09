using FluentValidation;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Validators;

/// <summary>
/// Regras do upload de fatura: um PDF (até 20 MB) ou de 1 a 10 imagens JPEG/PNG/WebP
/// (até 5 MB cada e 40 MB no total), sem misturar tipos. O conteúdo precisa ter a assinatura
/// do tipo declarado.
/// </summary>
public class CreateInvoiceImportValidator : AbstractValidator<CreateInvoiceImportRequestDto>
{
    public const string PdfContentType = "application/pdf";
    public const long MaxPdfBytes = 20L * 1024 * 1024;
    public const int MaxImages = 10;
    public const long MaxImageBytes = 5L * 1024 * 1024;
    public const long MaxTotalImageBytes = 40L * 1024 * 1024;

    /// <summary>Limite do corpo da requisição multipart (maior lote permitido + margem do multipart).</summary>
    public const long MaxRequestBytes = 42L * 1024 * 1024;

    public static readonly IReadOnlySet<string> ImageContentTypes =
        new HashSet<string>(StringComparer.Ordinal) { "image/jpeg", "image/png", "image/webp" };

    public const string UnsupportedTypeMessage =
        "Tipo de arquivo não suportado. Envie a fatura em PDF ou em imagens JPEG, PNG ou WebP.";

    public CreateInvoiceImportValidator()
    {
        RuleFor(x => x.CreditCardId)
            .NotEmpty().WithMessage("Informe o cartão da fatura.")
            .Matches("^[0-9a-fA-F]{24}$").WithMessage("Cartão inválido.");

        RuleFor(x => x.Files)
            .Must(f => f is { Count: > 0 }).WithMessage("Envie a fatura em PDF ou em imagens.");

        RuleForEach(x => x.Files)
            .Must(f => f.SizeBytes > 0).WithMessage("Um dos arquivos enviados está vazio.")
            .Must(f => ResolveSourceType(f.ContentType) is not null).WithMessage(UnsupportedTypeMessage);

        When(x => x.Files is { Count: > 0 } && AllSupported(x.Files), () =>
        {
            RuleFor(x => x.Files)
                .Must(f => f.Select(file => ResolveSourceType(file.ContentType)).Distinct().Count() == 1)
                .WithMessage("Envie um único PDF ou só imagens, sem misturar os tipos de arquivo.");

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
                    .WithMessage("Cada imagem pode ter no máximo 5 MB.")
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
        return null;
    }

    private static bool AllSupported(List<InvoiceImportUploadFileRequestDto> files) =>
        files.All(f => ResolveSourceType(f.ContentType) is not null);

    private static bool IsSingleType(List<InvoiceImportUploadFileRequestDto> files, InvoiceImportSourceType type) =>
        files is { Count: > 0 } && files.All(f => ResolveSourceType(f.ContentType) == type);

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
