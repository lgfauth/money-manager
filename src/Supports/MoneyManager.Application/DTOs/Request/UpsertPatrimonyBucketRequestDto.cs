namespace MoneyManager.Application.DTOs.Request;

public class UpsertPatrimonyBucketRequestDto
{
    public string Type { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public DateTime InitialBalanceDate { get; set; }
    public List<string> TrackedCategoryIds { get; set; } = [];
    public decimal ExpectedAnnualRate { get; set; }
}
