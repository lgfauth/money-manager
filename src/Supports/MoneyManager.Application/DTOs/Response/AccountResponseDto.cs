using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class AccountResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Color { get; set; } = "#00C896";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
