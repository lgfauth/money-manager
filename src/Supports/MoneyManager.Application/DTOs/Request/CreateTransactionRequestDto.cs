using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

public class CreateTransactionRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    public string? ToAccountId { get; set; }
    public TransactionStatus Status { get; set; }
    public string? ClientRequestId { get; set; }
    public bool SkipAccountBalanceImpact { get; set; }
    public bool SkipCommittedCreditImpact { get; set; }
    public bool SkipCreditLimitValidation { get; set; }
    public string? InstallmentGroupId { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? InstallmentCount { get; set; }
}

public class InstallmentPurchaseRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public bool FirstInstallmentInCurrentInvoice { get; set; } = true;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    public string? ClientRequestId { get; set; }
}
