using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class TransactionResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string AccountColor { get; set; } = "#00C896";
    public string? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#64748b";
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string? Notes { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
