namespace MoneyManager.Application.DTOs.Response;

public class BankMcpConnectionDto
{
    public string ItemId { get; set; } = string.Empty;
    public string ConnectorId { get; set; } = string.Empty;
    public string ConnectorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool AlreadyRegistered { get; set; }
    public bool PendingSetup { get; set; }
    public string? PendingConnectionId { get; set; }
}
