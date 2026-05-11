namespace MoneyManager.Application.DTOs.Response;

public record ReceiptAnalysisResultDto(
    string Description,
    decimal Amount,
    DateOnly Date,
    string TransactionType,
    string? CategoryHint,
    string? PaymentMethod,
    int? Installments,
    string? Notes,
    decimal Confidence
);
