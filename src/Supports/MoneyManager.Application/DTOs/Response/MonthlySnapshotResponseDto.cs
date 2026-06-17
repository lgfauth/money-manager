namespace MoneyManager.Application.DTOs.Response;

public class MonthlySnapshotResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string BucketId { get; set; } = string.Empty;
    public string ReferenceMonth { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal TrackedContributions { get; set; }
    public decimal EstimatedYield { get; set; }
    public decimal EstimatedClosingBalance { get; set; }
    public decimal? ConfirmedClosingBalance { get; set; }
    public decimal EffectiveBalance { get; set; }
    public List<string> TrackedCategoryIds { get; set; } = [];
    public bool Unconfirmed { get; set; }
    public bool DismissedByUser { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
