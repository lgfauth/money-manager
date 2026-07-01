namespace MoneyManager.Application.DTOs.Response;

public class BankMcpConnectionDto
{
    public string ExternalConnectionId { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string? InstitutionLogo { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool AlreadyRegistered { get; set; }
}
