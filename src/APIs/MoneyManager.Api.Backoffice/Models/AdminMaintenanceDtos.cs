namespace MoneyManager.Api.Administration.Models;

public sealed class AdminTargetUserRequest
{
    public string TargetUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public sealed class AdminAuditActionItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string OperatorUsername { get; set; } = string.Empty;
    public string TargetUserId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ParametersJson { get; set; }
    public string? ResultJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AdminMonthlyAuditReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalActions { get; set; }
    public int SuccessfulActions { get; set; }
    public int FailedActions { get; set; }
    public decimal SuccessRate { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> RoleCounts { get; set; } = new();
    public int UniqueOperators { get; set; }
    public int UniqueTargetUsers { get; set; }
    public List<AdminAuditActionItemDto> TopActions { get; set; } = new();
}

public sealed class AdminAuditActionSummaryDto
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
    public decimal SuccessRate { get; set; }
}
