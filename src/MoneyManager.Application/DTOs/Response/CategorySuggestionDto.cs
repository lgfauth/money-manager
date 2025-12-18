namespace MoneyManager.Application.DTOs.Response;

public class CategorySuggestionDto
{
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
