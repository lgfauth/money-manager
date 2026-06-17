namespace MoneyManager.Application.DTOs.Request;

public class ConfirmSnapshotRequestDto
{
    public List<BucketConfirmationDto> Buckets { get; set; } = [];
}

public class BucketConfirmationDto
{
    public string BucketId { get; set; } = string.Empty;
    public decimal ConfirmedBalance { get; set; }
}
