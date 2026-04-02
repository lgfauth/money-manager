using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.DTOs.Response;

public class CategoryResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
