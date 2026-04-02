namespace MoneyManager.Application.DTOs.Request;

public class CreateUserReportRequestDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
