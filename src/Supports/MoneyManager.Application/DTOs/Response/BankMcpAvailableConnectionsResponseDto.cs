namespace MoneyManager.Application.DTOs.Response;

public class BankMcpAvailableConnectionsResponseDto
{
    public List<BankMcpConnectionDto> Connections { get; set; } = [];
    public string AddConnectionUrl { get; set; } = string.Empty;
}
