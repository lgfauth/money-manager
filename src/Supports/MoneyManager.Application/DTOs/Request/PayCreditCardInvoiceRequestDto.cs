namespace MoneyManager.Application.DTOs.Request;

public class PayCreditCardInvoiceRequestDto
{
    public string PaidWithAccountId { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public DateTime PaidAt { get; set; }
}
