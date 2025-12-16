namespace MoneyManager.Application.DTOs.Response;

public class TransactionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
