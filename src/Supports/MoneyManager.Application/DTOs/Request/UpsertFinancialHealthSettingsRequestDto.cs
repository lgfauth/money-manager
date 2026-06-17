namespace MoneyManager.Application.DTOs.Request;

public class UpsertFinancialHealthSettingsRequestDto
{
    public string ModeName { get; set; } = string.Empty;
    public int InvestPercent { get; set; }
    public int ReserveMonths { get; set; }
    public int FireMultiplier { get; set; }
    public int FixedExpensePercent { get; set; }
    public int InstallmentPercent { get; set; }
}
