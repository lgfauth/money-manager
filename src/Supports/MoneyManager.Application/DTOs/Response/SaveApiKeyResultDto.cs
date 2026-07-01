namespace MoneyManager.Application.DTOs.Response;

public class SaveApiKeyResultDto
{
    public bool IsValid { get; set; }
    public int AvailableConnections { get; set; }
}
