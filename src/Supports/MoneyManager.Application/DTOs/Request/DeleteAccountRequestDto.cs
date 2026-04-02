namespace MoneyManager.Application.DTOs.Request;

public class DeleteAccountRequestDto
{
    public string Password { get; set; } = string.Empty;
    public string ConfirmationText { get; set; } = string.Empty;
}
