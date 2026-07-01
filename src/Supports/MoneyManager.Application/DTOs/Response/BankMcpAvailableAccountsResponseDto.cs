namespace MoneyManager.Application.DTOs.Response;

public class BankMcpAvailableAccountsResponseDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public List<BankMcpAccountDto> Accounts { get; set; } = [];
}

public class BankMcpAccountDto
{
    public string AccountId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Subtype { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
