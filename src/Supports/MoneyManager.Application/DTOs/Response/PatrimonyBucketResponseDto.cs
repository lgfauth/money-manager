namespace MoneyManager.Application.DTOs.Response;

public class PatrimonyBucketResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public DateTime InitialBalanceDate { get; set; }
    public List<string> TrackedCategoryIds { get; set; } = [];
    public decimal ExpectedAnnualRate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
