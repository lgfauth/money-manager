namespace MoneyManager.Application.DTOs.Response;

public class HealthScoreResponseDto
{
    public bool HasData { get; set; }
    public int OverallScore { get; set; }
    public string ReferenceMonth { get; set; } = string.Empty;
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalInvestments { get; set; }
    public MetricScoreDto InvestmentMetric { get; set; } = new();
    public MetricScoreDto ReserveMetric { get; set; } = new();
    public MetricScoreDto FireMetric { get; set; } = new();
    public MetricScoreDto ExpenseMetric { get; set; } = new();
    public FireProjectionDto Projection { get; set; } = new();
}

public class MetricScoreDto
{
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal ProgressPercent { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FireProjectionDto
{
    public decimal FireTarget { get; set; }
    public decimal ReserveTarget { get; set; }
    public decimal CurrentFireBalance { get; set; }
    public decimal CurrentReserveBalance { get; set; }
    public int? EstimatedMonthsToFire { get; set; }
}
