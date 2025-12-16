namespace MoneyManager.Application.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string email);
}
