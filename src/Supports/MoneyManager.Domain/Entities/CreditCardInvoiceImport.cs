using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

/// <summary>
/// Job de importação de uma fatura fechada de cartão de crédito (PDF, imagens ou planilha).
/// Os arquivos enviados ficam em <see cref="CreditCardInvoiceImportFile"/> só até o fim do processamento.
/// </summary>
[BsonIgnoreExtraElements]
public class CreditCardInvoiceImport
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("creditCardId")]
    public string CreditCardId { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportStatus Status { get; set; } = InvoiceImportStatus.Queued;

    [BsonElement("sourceType")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportSourceType SourceType { get; set; }

    [BsonElement("fileCount")]
    public int FileCount { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    // Mensagem amigável (em português) exibida ao usuário quando o job falha.
    [BsonElement("errorMessage")]
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }

    // Mês de referência (yyyy-MM) da fatura importada; também fica no topo do documento
    // para a consulta de importação já confirmada do mesmo cartão e mês.
    [BsonElement("referenceMonth")]
    [BsonIgnoreIfNull]
    public string? ReferenceMonth { get; set; }

    [BsonElement("invoice")]
    [BsonIgnoreIfNull]
    public InvoiceImportDocumentInfo? Invoice { get; set; }

    [BsonElement("holders")]
    public List<InvoiceImportHolder> Holders { get; set; } = [];

    [BsonElement("warnings")]
    public List<InvoiceImportWarning> Warnings { get; set; } = [];

    [BsonElement("items")]
    public List<InvoiceImportItem> Items { get; set; } = [];

    // Nomes dos titulares escolhidos na confirmação (base da pré-seleção da próxima importação).
    [BsonElement("selectedHolders")]
    public List<string> SelectedHolders { get; set; } = [];

    [BsonElement("result")]
    [BsonIgnoreIfNull]
    public InvoiceImportConfirmationResult? Result { get; set; }

    [BsonElement("processingStartedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ProcessingStartedAt { get; set; }

    [BsonElement("processedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ProcessedAt { get; set; }

    [BsonElement("confirmedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ConfirmedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("version")]
    public int Version { get; set; } = 1;

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }
}

/// <summary>
/// Dados da fatura extraídos do documento.
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportDocumentInfo
{
    [BsonElement("issuer")]
    [BsonIgnoreIfNull]
    public string? Issuer { get; set; }

    [BsonElement("cardLastDigits")]
    [BsonIgnoreIfNull]
    public string? CardLastDigits { get; set; }

    [BsonElement("closingDate")]
    [BsonIgnoreIfNull]
    public DateTime? ClosingDate { get; set; }

    // true quando o documento não trazia fechamento e ele foi calculado pelo ClosingDay do cartão.
    [BsonElement("closingDateCalculated")]
    public bool ClosingDateCalculated { get; set; }

    [BsonElement("dueDate")]
    [BsonIgnoreIfNull]
    public DateTime? DueDate { get; set; }

    [BsonElement("referenceMonth")]
    [BsonIgnoreIfNull]
    public string? ReferenceMonth { get; set; }

    [BsonElement("totalAmount")]
    [BsonIgnoreIfNull]
    public decimal? TotalAmount { get; set; }

    // Documento que se declara fatura "aberta" ou "parcial".
    [BsonElement("isOpenOrPartial")]
    public bool IsOpenOrPartial { get; set; }
}

/// <summary>
/// Titular (ou adicional) encontrado na fatura, com o resumo dos seus lançamentos.
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportHolder
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportHolderType Type { get; set; } = InvoiceImportHolderType.Unknown;

    [BsonElement("cardLastDigits")]
    public List<string> CardLastDigits { get; set; } = [];

    [BsonElement("itemCount")]
    public int ItemCount { get; set; }

    [BsonElement("totalAmount")]
    public decimal TotalAmount { get; set; }

    // Titular sugerido como selecionado na revisão.
    [BsonElement("isSuggested")]
    public bool IsSuggested { get; set; }
}

/// <summary>
/// Alerta geral da importação (não ligado a um item específico).
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportWarning
{
    [BsonElement("code")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportWarningCode Code { get; set; }

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;

    [BsonElement("isBlocking")]
    public bool IsBlocking { get; set; }
}

/// <summary>
/// Lançamento extraído da fatura, pendente de revisão do usuário.
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportItem
{
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [BsonElement("date")]
    [BsonIgnoreIfNull]
    public DateTime? Date { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public CreditCardTransactionType Type { get; set; } = CreditCardTransactionType.Purchase;

    [BsonElement("installmentNumber")]
    [BsonIgnoreIfNull]
    public int? InstallmentNumber { get; set; }

    [BsonElement("totalInstallments")]
    [BsonIgnoreIfNull]
    public int? TotalInstallments { get; set; }

    [BsonElement("holderName")]
    [BsonIgnoreIfNull]
    public string? HolderName { get; set; }

    [BsonElement("holderType")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportHolderType HolderType { get; set; } = InvoiceImportHolderType.Unknown;

    [BsonElement("cardLastDigits")]
    [BsonIgnoreIfNull]
    public string? CardLastDigits { get; set; }

    [BsonElement("cardType")]
    [BsonRepresentation(BsonType.String)]
    public InvoiceImportCardType CardType { get; set; } = InvoiceImportCardType.Unknown;

    [BsonElement("source")]
    [BsonIgnoreIfNull]
    public InvoiceImportSourceReference? Source { get; set; }

    [BsonElement("categoryHint")]
    [BsonIgnoreIfNull]
    public string? CategoryHint { get; set; }

    // Categoria de despesa do usuário associada à dica da IA, quando houver correspondência.
    [BsonElement("categoryId")]
    [BsonIgnoreIfNull]
    public string? CategoryId { get; set; }

    [BsonElement("confidence")]
    [BsonIgnoreIfNull]
    public decimal? Confidence { get; set; }

    // Observação livre do item (ex.: moeda estrangeira e cotação).
    [BsonElement("notes")]
    [BsonIgnoreIfNull]
    public string? Notes { get; set; }

    [BsonElement("warnings")]
    [BsonRepresentation(BsonType.String)]
    public List<InvoiceImportWarningCode> Warnings { get; set; } = [];

    [BsonElement("isSelectedByDefault")]
    public bool IsSelectedByDefault { get; set; } = true;

    [BsonElement("fingerprint")]
    [BsonIgnoreIfNull]
    public string? Fingerprint { get; set; }
}

/// <summary>
/// Onde o item foi encontrado no documento: arquivo (imagem) e página, ou aba e linha da planilha.
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportSourceReference
{
    // Posição do arquivo no upload (0 = primeiro), relevante para imagens.
    [BsonElement("fileIndex")]
    [BsonIgnoreIfNull]
    public int? FileIndex { get; set; }

    [BsonElement("page")]
    [BsonIgnoreIfNull]
    public int? Page { get; set; }

    [BsonElement("sheet")]
    [BsonIgnoreIfNull]
    public string? Sheet { get; set; }

    [BsonElement("row")]
    [BsonIgnoreIfNull]
    public int? Row { get; set; }
}

/// <summary>
/// Contadores do resultado da confirmação.
/// </summary>
[BsonIgnoreExtraElements]
public class InvoiceImportConfirmationResult
{
    [BsonElement("createdCount")]
    public int CreatedCount { get; set; }

    [BsonElement("skippedCount")]
    public int SkippedCount { get; set; }

    [BsonElement("affectedInvoiceIds")]
    public List<string> AffectedInvoiceIds { get; set; } = [];
}
