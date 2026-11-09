namespace MoneyManager.Application.DTOs.Response;

/// <summary>Resposta do upload (202): id e status inicial do job.</summary>
public class InvoiceImportCreatedResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>Cartão que pode receber importação de fatura (não sincronizado pelo Open Finance).</summary>
public class InvoiceImportEligibleCardResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string Color { get; set; } = string.Empty;
    public int ClosingDay { get; set; }
    public int BillingDueDay { get; set; }
}

/// <summary>Importação de fatura com o resultado da extração, para acompanhamento e revisão.</summary>
public class CreditCardInvoiceImportResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string CreditCardId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReferenceMonth { get; set; }
    public InvoiceImportDocumentInfoResponseDto? Invoice { get; set; }
    public List<InvoiceImportHolderResponseDto> Holders { get; set; } = [];
    public List<InvoiceImportWarningResponseDto> Warnings { get; set; } = [];
    public List<InvoiceImportItemResponseDto> Items { get; set; } = [];
    public List<string> SelectedHolders { get; set; } = [];
    public InvoiceImportConfirmationResultResponseDto? Result { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int Version { get; set; }
}

public class InvoiceImportDocumentInfoResponseDto
{
    public string? Issuer { get; set; }
    public string? CardLastDigits { get; set; }
    public DateTime? ClosingDate { get; set; }
    public bool ClosingDateCalculated { get; set; }
    public DateTime? DueDate { get; set; }
    public string? ReferenceMonth { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool IsOpenOrPartial { get; set; }
}

public class InvoiceImportHolderResponseDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> CardLastDigits { get; set; } = [];
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsSuggested { get; set; }
}

public class InvoiceImportWarningResponseDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
}

public class InvoiceImportItemWarningResponseDto
{
    public string Code { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
}

public class InvoiceImportItemResponseDto
{
    public string Id { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public int? InstallmentNumber { get; set; }
    public int? TotalInstallments { get; set; }
    public string? HolderName { get; set; }
    public string HolderType { get; set; } = string.Empty;
    public string? CardLastDigits { get; set; }
    public string CardType { get; set; } = string.Empty;
    public InvoiceImportSourceResponseDto? Source { get; set; }
    public string? CategoryHint { get; set; }
    public string? CategoryId { get; set; }
    public decimal? Confidence { get; set; }
    public string? Notes { get; set; }
    public List<InvoiceImportItemWarningResponseDto> Warnings { get; set; } = [];
    public bool IsSelectedByDefault { get; set; }
}

public class InvoiceImportSourceResponseDto
{
    public int? FileIndex { get; set; }
    public int? Page { get; set; }
    public string? Sheet { get; set; }
    public int? Row { get; set; }
}

public class InvoiceImportConfirmationResultResponseDto
{
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> AffectedInvoiceIds { get; set; } = [];
}
