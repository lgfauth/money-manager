namespace MoneyManager.Application.DTOs.Response;

public class BankMcpAvailableAccountsResponseDto
{
    public string ConnectionId { get; set; } = string.Empty;
    public List<BankMcpAccountDto> Accounts { get; set; } = [];
}

public class BankMcpAccountDto
{
    public string ExternalAccountId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
