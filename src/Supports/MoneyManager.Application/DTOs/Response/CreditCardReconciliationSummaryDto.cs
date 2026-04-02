namespace MoneyManager.Application.DTOs.Response;

public class CreditCardReconciliationSummaryDto
{
    public int AccountsProcessed { get; set; }
    public int InvoicesRecalculated { get; set; }
    public int AccountsUpdated { get; set; }
    public decimal TotalCommittedCredit { get; set; }
}