namespace MoneyManager.Application.DTOs.Request;

public class UpdateProfileRequestDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? ProfilePicture { get; set; }
    public string? PreferredLanguage { get; set; }
}

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UpdateEmailRequestDto
{
    public string NewEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
