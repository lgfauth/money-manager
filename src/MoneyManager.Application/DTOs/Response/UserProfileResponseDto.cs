namespace MoneyManager.Application.DTOs.Response;

public class UserProfileResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
}
