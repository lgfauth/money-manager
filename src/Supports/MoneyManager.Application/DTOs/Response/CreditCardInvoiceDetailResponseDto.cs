namespace MoneyManager.Application.DTOs.Response;

public class CreditCardInvoiceDetailResponseDto
{
    public CreditCardInvoiceResponseDto Invoice { get; set; } = new();
    public List<CreditCardTransactionResponseDto> Transactions { get; set; } = [];
}
