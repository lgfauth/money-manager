namespace MoneyManager.Application.DTOs.Response;

public class SnapshotStatusResponseDto
{
    public bool HasConfiguration { get; set; }
    public bool ShowBanner { get; set; }
    public string? ReferenceMonth { get; set; }
    public List<PendingBucketStatusDto> PendingBuckets { get; set; } = [];
}

public class PendingBucketStatusDto
{
    public string BucketId { get; set; } = string.Empty;
    public string BucketType { get; set; } = string.Empty;
    public decimal EstimatedBalance { get; set; }
    public decimal TrackedContributions { get; set; }
    public decimal EstimatedYield { get; set; }
}
