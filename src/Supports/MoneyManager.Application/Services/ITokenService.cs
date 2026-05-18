using System.Security.Claims;

namespace MoneyManager.Application.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string name);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
