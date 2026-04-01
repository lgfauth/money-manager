using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MoneyManager.Api.Administration.Models;

namespace MoneyManager.Api.Administration.Services;

public sealed class AdminTokenService
{
    private readonly IConfiguration _configuration;

    public AdminTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool ValidateCredentials(string username, string password)
    {
        return TryValidateCredentials(username, password, out _);
    }

    public bool TryValidateCredentials(string username, string password, out string role)
    {
        role = AdminRoles.Viewer;

        var configuredUsers = _configuration.GetSection("AdminAuth:Users").Get<List<AdminUserCredential>>() ?? [];
        if (configuredUsers.Count > 0)
        {
            var matchingUser = configuredUsers.FirstOrDefault(user =>
                string.Equals(user.Username, username, StringComparison.Ordinal)
                && string.Equals(user.Password, password, StringComparison.Ordinal)
                && AdminRoles.IsValid(user.Role));

            if (matchingUser is null)
            {
                return false;
            }

            role = matchingUser.Role.ToLowerInvariant();
            return true;
        }

        var expectedUsername = _configuration["AdminAuth:Username"]
            ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME")
            ?? "admin";

        var expectedPassword = _configuration["AdminAuth:Password"]
            ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? string.Empty;

        var expectedRole = _configuration["AdminAuth:Role"]
            ?? Environment.GetEnvironmentVariable("ADMIN_ROLE")
            ?? AdminRoles.Admin;

        if (string.IsNullOrWhiteSpace(expectedPassword))
        {
            return false;
        }

        var valid = string.Equals(username, expectedUsername, StringComparison.Ordinal)
            && string.Equals(password, expectedPassword, StringComparison.Ordinal);

        if (valid)
        {
            role = AdminRoles.IsValid(expectedRole)
                ? expectedRole.ToLowerInvariant()
                : AdminRoles.Admin;
        }

        return valid;
    }

    public AdminLoginResponse GenerateToken(string username, string role)
    {
        var issuer = _configuration["AdminAuth:Issuer"] ?? "MoneyManager.Admin";
        var audience = _configuration["AdminAuth:Audience"] ?? "MoneyManager.Admin.Users";
        var secret = _configuration["AdminAuth:SecretKey"]
            ?? Environment.GetEnvironmentVariable("ADMIN_AUTH_SECRET")
            ?? "change-this-admin-secret-key-with-at-least-32-characters";

        var expirationMinutesRaw = _configuration["AdminAuth:TokenExpirationMinutes"];
        var expirationMinutes = int.TryParse(expirationMinutesRaw, out var parsed)
            ? parsed
            : 60;

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            ]),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(tokenDescriptor);

        return new AdminLoginResponse
        {
            AccessToken = handler.WriteToken(token),
            ExpiresAtUtc = expiresAt,
            Role = role
        };
    }
}
