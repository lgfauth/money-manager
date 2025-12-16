namespace MoneyManager.Application.DTOs.Response;

public class SummaryResponseDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
}
