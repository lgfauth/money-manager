namespace MoneyManager.Application.DTOs.Response;

public class FinancialHealthSettingsResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ModeName { get; set; } = string.Empty;
    public int InvestPercent { get; set; }
    public int ReserveMonths { get; set; }
    public int FireMultiplier { get; set; }
    public int FixedExpensePercent { get; set; }
    public int InstallmentPercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
