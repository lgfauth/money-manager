namespace MoneyManager.Application.DTOs.Response;

public class SummaryResponseDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<CurrencySummaryDto> ByCurrency { get; set; } = [];
}

public class CurrencySummaryDto
{
    public string Currency { get; set; } = "BRL";
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}
