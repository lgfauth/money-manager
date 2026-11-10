using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

/// <summary>
/// Extrai dados e lançamentos de uma fatura de cartão (PDF, imagens ou texto de planilha) usando IA.
/// Lança <see cref="MoneyManager.Domain.Exceptions.InvoiceDocumentAnalysisException"/> em caso de falha,
/// indicando se ela é temporária ou definitiva.
/// </summary>
public interface IInvoiceDocumentAnalysisService
{
    Task<InvoiceDocumentAnalysisResultDto> AnalyzeAsync(
        InvoiceDocumentAnalysisRequestDto request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Arquivo já normalizado: <c>application/pdf</c>, <c>image/*</c> ou <c>text/plain</c> (tabela da planilha, UTF-8).
/// </summary>
public sealed record InvoiceDocumentFileDto(string ContentType, byte[] Content);

/// <summary>
/// Entrada da análise. <paramref name="Today"/> é a data de hoje informada à IA (sem ela o modelo não sabe a data atual).
/// <paramref name="CategoryNames"/> são as categorias de despesa entre as quais a IA escolhe a de cada item
/// (vazio ou null: a IA não sugere categoria).
/// </summary>
public sealed record InvoiceDocumentAnalysisRequestDto(
    InvoiceImportSourceType SourceType,
    IReadOnlyList<InvoiceDocumentFileDto> Files,
    DateOnly Today,
    IReadOnlyList<string>? CategoryNames = null);

/// <summary>
/// Data lida do documento. <paramref name="Year"/> é null quando o documento não mostra o ano
/// (ex.: "12/10"); a regra de ano é aplicada depois, na montagem da revisão.
/// </summary>
public sealed record InvoiceDocumentDateDto(int Day, int Month, int? Year);

public sealed record InvoiceDocumentInfoDto(
    string? Issuer,
    string? CardLastDigits,
    InvoiceDocumentDateDto? ClosingDate,
    InvoiceDocumentDateDto? DueDate,
    decimal? TotalAmount,
    bool IsOpenOrPartial);

/// <summary>
/// Onde o item aparece: <paramref name="FileIndex"/> (0 = primeiro arquivo) e página, ou aba e linha da planilha.
/// </summary>
public sealed record InvoiceDocumentSourceDto(int? FileIndex, int? Page, string? Sheet, int? Row);

/// <summary>
/// Lançamento extraído. <paramref name="Amount"/> é sempre positivo; estornos vêm com
/// <paramref name="Type"/> = <see cref="CreditCardTransactionType.Refund"/>.
/// </summary>
public sealed record InvoiceDocumentItemDto(
    InvoiceDocumentDateDto? Date,
    string Description,
    decimal? Amount,
    CreditCardTransactionType Type,
    int? InstallmentNumber,
    int? TotalInstallments,
    string? HolderName,
    InvoiceImportHolderType HolderType,
    string? CardLastDigits,
    InvoiceImportCardType CardType,
    InvoiceDocumentSourceDto? Source,
    string? CategoryHint,
    decimal? Confidence,
    string? Notes);

/// <summary>
/// Resultado da análise: dados da fatura, lançamentos e pendências (trechos que a IA não conseguiu ler).
/// </summary>
public sealed record InvoiceDocumentAnalysisResultDto(
    InvoiceDocumentInfoDto Invoice,
    IReadOnlyList<InvoiceDocumentItemDto> Items,
    IReadOnlyList<string> Issues);
