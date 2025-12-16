namespace MoneyManager.Application.DTOs.Request;

public class CreateBudgetRequestDto
{
    public string Month { get; set; } = string.Empty; // YYYY-MM format
    public List<BudgetItemRequestDto> Items { get; set; } = [];
}

public class BudgetItemRequestDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
}
