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
    private readonly ILogger<AdminTokenService> _logger;

    public AdminTokenService(IConfiguration configuration, ILogger<AdminTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool ValidateCredentials(string username, string password)
    {
        return TryValidateCredentials(username, password, out _);
    }

    public bool TryValidateCredentials(string username, string password, out string role)
    {
        role = AdminRoles.Viewer;

        _logger.LogInformation(
            "Validating admin credentials. RequestUsername={RequestUsername}, RequestPasswordLength={RequestPasswordLength}",
            username,
            password?.Length ?? 0);

        var configuredUsers = _configuration.GetSection("AdminAuth:Users").Get<List<AdminUserCredential>>() ?? [];
        _logger.LogInformation("AdminAuth:Users configured count={UsersCount}", configuredUsers.Count);

        if (configuredUsers.Count > 0)
        {
            var usersDebug = configuredUsers.Select(user => new
            {
                Username = user.Username,
                Role = user.Role,
                HasPassword = !string.IsNullOrWhiteSpace(user.Password),
                PasswordLength = user.Password?.Length ?? 0
            }).ToList();

            _logger.LogInformation("Using AdminAuth:Users credentials source. Users={UsersDebug}", usersDebug);

            var matchingUser = configuredUsers.FirstOrDefault(user =>
                string.Equals(user.Username, username, StringComparison.Ordinal)
                && string.Equals(user.Password, password, StringComparison.Ordinal)
                && AdminRoles.IsValid(user.Role));

            if (matchingUser is null)
            {
                _logger.LogWarning("No matching user found in AdminAuth:Users for RequestUsername={RequestUsername}", username);
                return false;
            }

            role = matchingUser.Role.ToLowerInvariant();
            _logger.LogInformation(
                "Matched user in AdminAuth:Users. Username={Username}, Role={Role}",
                matchingUser.Username,
                role);
            return true;
        }

        var configUsername = _configuration["AdminAuth:Username"];
        var envUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");
        var expectedUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME")
            ?? _configuration["AdminAuth:Username"]
            ?? "admin";

        var configPassword = _configuration["AdminAuth:Password"];
        var envPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        var expectedPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
            ?? _configuration["AdminAuth:Password"]
            ?? string.Empty;

        var configRole = _configuration["AdminAuth:Role"];
        var envRole = Environment.GetEnvironmentVariable("ADMIN_ROLE");
        var expectedRole = Environment.GetEnvironmentVariable("ADMIN_ROLE")
            ?? _configuration["AdminAuth:Role"]
            ?? AdminRoles.Admin;

        _logger.LogInformation(
            "Using single-user credentials source. ConfigUsernameSet={ConfigUsernameSet}, EnvUsernameSet={EnvUsernameSet}, EffectiveUsername={EffectiveUsername}, ConfigPasswordSet={ConfigPasswordSet}, EnvPasswordSet={EnvPasswordSet}, EffectivePasswordLength={EffectivePasswordLength}, ConfigRoleSet={ConfigRoleSet}, EnvRoleSet={EnvRoleSet}, EffectiveRole={EffectiveRole}",
            !string.IsNullOrWhiteSpace(configUsername),
            !string.IsNullOrWhiteSpace(envUsername),
            expectedUsername,
            !string.IsNullOrWhiteSpace(configPassword),
            !string.IsNullOrWhiteSpace(envPassword),
            expectedPassword.Length,
            !string.IsNullOrWhiteSpace(configRole),
            !string.IsNullOrWhiteSpace(envRole),
            expectedRole);

        if (string.IsNullOrWhiteSpace(expectedPassword))
        {
            _logger.LogWarning("Expected admin password is empty. Login will always fail.");
            return false;
        }

        var valid = string.Equals(username, expectedUsername, StringComparison.Ordinal)
            && string.Equals(password, expectedPassword, StringComparison.Ordinal);

        _logger.LogInformation(
            "Single-user credential comparison result. UsernameMatched={UsernameMatched}, PasswordMatched={PasswordMatched}, FinalValid={FinalValid}",
            string.Equals(username, expectedUsername, StringComparison.Ordinal),
            string.Equals(password, expectedPassword, StringComparison.Ordinal),
            valid);

        if (valid)
        {
            role = AdminRoles.IsValid(expectedRole)
                ? expectedRole.ToLowerInvariant()
                : AdminRoles.Admin;

            _logger.LogInformation("Admin role resolved after successful login. Role={Role}", role);
        }

        return valid;
    }

    public AdminLoginResponse GenerateToken(string username, string role)
    {
        var issuer = _configuration["AdminAuth:Issuer"] ?? "MoneyManager.Admin";
        var audience = _configuration["AdminAuth:Audience"] ?? "MoneyManager.Admin.Users";
        var secret = Environment.GetEnvironmentVariable("ADMIN_AUTH_SECRET")
            ?? _configuration["AdminAuth:SecretKey"]
            ?? "change-this-admin-secret-key-with-at-least-32-characters";

        var expirationMinutesRaw = _configuration["AdminAuth:TokenExpirationMinutes"];
        var expirationMinutes = int.TryParse(expirationMinutesRaw, out var parsed)
            ? parsed
            : 60;

        _logger.LogInformation(
            "Generating admin JWT. Username={Username}, Role={Role}, Issuer={Issuer}, Audience={Audience}, SecretLength={SecretLength}, ExpirationMinutes={ExpirationMinutes}",
            username,
            role,
            issuer,
            audience,
            secret.Length,
            expirationMinutes);

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
