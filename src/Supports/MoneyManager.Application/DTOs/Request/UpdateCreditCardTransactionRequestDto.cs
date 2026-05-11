namespace MoneyManager.Application.DTOs.Request;

public class UpdateCreditCardTransactionRequestDto
{
    public string Description { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
}
