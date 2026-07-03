using System.Security.Claims;

namespace MoneyManager.Application.Services;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string name, IEnumerable<Claim>? extraClaims = null);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
