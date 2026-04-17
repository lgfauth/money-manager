namespace MoneyManager.Application.DTOs.Request;

public class CreateCreditCardRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int BillingDueDay { get; set; }
    public int? BestPurchaseDay { get; set; }
    public string Color { get; set; } = "#4E9BFF";
    public string Currency { get; set; } = "BRL";
}
