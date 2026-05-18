using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using FluentValidation;
using MoneyManager.Presentation.Extensions;
using System.IdentityModel.Tokens.Jwt;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<LoginRequestDto> _loginValidator;
    private readonly ITokenBlacklistService _blacklist;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<LoginRequestDto> loginValidator,
        ITokenBlacklistService blacklist)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _blacklist = blacklist;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            // Após registro, faz login automático para criar a sessão via cookie
            var registered = await _authService.RegisterAsync(request);
            var loggedIn = await _authService.LoginAsync(new LoginRequestDto { Email = request.Email, Password = request.Password });

            SetAuthCookies(loggedIn.Token!, loggedIn.RefreshToken!);

            return Created(string.Empty, new { registered.Id, registered.Name, registered.Email });
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _authService.LoginAsync(request);

            SetAuthCookies(result.Token!, result.RefreshToken!);

            return Ok(new { result.Id, result.Name, result.Email });
        }
        catch (Exception ex)
        {
            return this.ApiUnauthorized(ex.Message);
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["mm_refresh_token"];
        if (string.IsNullOrEmpty(refreshToken))
            return this.ApiUnauthorized("Refresh token não encontrado");

        try
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            SetAuthCookies(result.Token!, result.RefreshToken!);
            return Ok(new { result.Id, result.Name, result.Email });
        }
        catch (UnauthorizedAccessException ex)
        {
            return this.ApiUnauthorized(ex.Message);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        return Ok(new { Id = userId, Name = name, Email = email });
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // Revoga o access token atual para que não seja reutilizado antes de expirar
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Expiration)?.Value
                    ?? User.FindFirst("exp")?.Value;
        if (jti != null && long.TryParse(expClaim, out var expUnix))
        {
            var expiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            _blacklist.Revoke(jti, expiry);
        }

        Response.Cookies.Delete("mm_access_token", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
        Response.Cookies.Delete("mm_refresh_token", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None });
        return NoContent();
    }

    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        Response.Cookies.Append("mm_access_token", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        Response.Cookies.Append("mm_refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });
    }
}
