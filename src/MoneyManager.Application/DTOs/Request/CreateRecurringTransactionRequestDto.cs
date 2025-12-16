using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

public class CreateRecurringTransactionRequestDto
{
    public string AccountId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public RecurrenceFrequency Frequency { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DayOfMonth { get; set; }
    public List<string> Tags { get; set; } = [];
}
