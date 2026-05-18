using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IProcessLogger _processLogger;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _processLogger = processLogger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        _processLogger.AddStep("Registration attempt", new Dictionary<string, object?>
        {
            ["email"] = request.Email
        });

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _processLogger.AddWarning("Registration failed: email already exists");
            throw new InvalidOperationException("Email already registered");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("User registered successfully", new Dictionary<string, object?>
        {
            ["userId"] = user.Id
        });

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        _processLogger.AddStep("Login attempt", new Dictionary<string, object?>
        {
            ["email"] = request.Email
        });

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _processLogger.AddWarning("Login failed: invalid credentials");
            throw new InvalidOperationException("Invalid credentials");
        }

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Name);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.Users.UpdateAsync(user);

        _processLogger.AddStep("User logged in successfully", new Dictionary<string, object?>
        {
            ["userId"] = user.Id
        });

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var user = users.FirstOrDefault(u =>
            u.RefreshToken == refreshToken &&
            u.RefreshTokenExpiry > DateTime.UtcNow &&
            !u.IsDeleted);

        if (user == null)
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado");

        var newAccessToken = _tokenService.GenerateToken(user.Id, user.Email, user.Name);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.Users.UpdateAsync(user);

        return new AuthResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Token = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}
