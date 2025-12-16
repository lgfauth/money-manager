using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using MoneyManager.Application.Services;

namespace MoneyManager.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationHours;

    public TokenService(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        _secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-key-that-is-long-enough-for-256-bits";
        _issuer = jwtSettings["Issuer"] ?? "MoneyManager";
        _audience = jwtSettings["Audience"] ?? "MoneyManagerUsers";
        _expirationHours = int.Parse(jwtSettings["ExpirationHours"] ?? "24");
    }

    public string GenerateToken(string userId, string email)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
