using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Color { get; set; } = "#00C896";
}
