namespace MoneyManager.Application.DTOs.Request;

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public decimal InitialBalance { get; set; }
}
