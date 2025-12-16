namespace MoneyManager.Application.DTOs.Request;

public class CreateTransactionRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public int Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? ToAccountId { get; set; }
    public int Status { get; set; }
}
