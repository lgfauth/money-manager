using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Request;

public class CreateCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Color { get; set; } = "#FF5733";
}
