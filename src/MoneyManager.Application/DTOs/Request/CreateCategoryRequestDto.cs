namespace MoneyManager.Application.DTOs.Request;

public class CreateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Color { get; set; } = "#FF5733";
}
