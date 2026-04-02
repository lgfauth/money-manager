using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AdminTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AdminTokenService tokenService, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var passwordLength = request.Password?.Length ?? 0;
        _logger.LogInformation(
            "Admin login attempt received. Username={Username}, PasswordLength={PasswordLength}, RemoteIp={RemoteIp}",
            request.Username,
            passwordLength,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            _logger.LogWarning(
                "Admin login rejected due to missing username/password. UsernameIsEmpty={UsernameIsEmpty}, PasswordIsEmpty={PasswordIsEmpty}",
                string.IsNullOrWhiteSpace(request.Username),
                string.IsNullOrWhiteSpace(request.Password));

            return BadRequest(new { message = "Username and password are required" });
        }

        var valid = _tokenService.TryValidateCredentials(request.Username, request.Password, out var role);
        if (!valid)
        {
            _logger.LogWarning("Admin login failed. Username={Username}", request.Username);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("Admin login successful. Username={Username}, Role={Role}", request.Username, role);
        var token = _tokenService.GenerateToken(request.Username, role);
        return Ok(token);
    }
}
