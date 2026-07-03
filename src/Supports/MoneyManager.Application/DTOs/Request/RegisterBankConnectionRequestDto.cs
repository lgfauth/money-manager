namespace MoneyManager.Application.DTOs.Request;

public class RegisterBankConnectionRequestDto
{
    public string ExternalConnectionId { get; set; } = string.Empty; // item_id do Banco MCP
}
