namespace MoneyManager.Application.DTOs.Response;

public class BankMcpAvailableConnectionsResponseDto
{
    public bool HasApiKey { get; set; }
    public bool ApiKeyExpired { get; set; }
    public List<BankMcpConnectionDto> Connections { get; set; } = [];
    public string AddConnectionUrl { get; set; } = string.Empty;
}
