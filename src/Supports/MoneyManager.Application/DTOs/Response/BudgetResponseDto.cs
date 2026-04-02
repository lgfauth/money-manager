namespace MoneyManager.Application.DTOs.Response;

public class BudgetResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public List<BudgetItemResponseDto> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class BudgetItemResponseDto
{
    public string CategoryId { get; set; } = string.Empty;
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
}
