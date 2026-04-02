using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class CategorySuggestionDto
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
