using System.Security.Claims;
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
    private readonly ISubscriptionService _subscriptionService;
    private readonly IProcessLogger _processLogger;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ISubscriptionService subscriptionService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _subscriptionService = subscriptionService;
        _processLogger = processLogger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        _processLogger.AddStep("Tentativa de registro", new Dictionary<string, object?>
        {
            ["email"] = request.Email
        });

        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            _processLogger.AddWarning("Registro falhou: e-mail já cadastrado");
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

        // Trial de 14 dias ativado automaticamente no registro
        await _subscriptionService.ActivateTrialAsync(user.Id);

        _processLogger.AddStep("Usuário registrado com sucesso", new Dictionary<string, object?>
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
        _processLogger.AddStep("Tentativa de login", new Dictionary<string, object?>
        {
            ["email"] = request.Email
        });

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _processLogger.AddWarning("Login falhou: credenciais inválidas");
            throw new InvalidOperationException("Invalid credentials");
        }

        var subscriptionClaims = await BuildSubscriptionClaimsAsync(user.Id);
        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Name, subscriptionClaims);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _unitOfWork.Users.UpdateAsync(user);

        _processLogger.AddStep("Usuário autenticado com sucesso", new Dictionary<string, object?>
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

        var subscriptionClaims = await BuildSubscriptionClaimsAsync(user.Id);
        var newAccessToken = _tokenService.GenerateToken(user.Id, user.Email, user.Name, subscriptionClaims);
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

    private async Task<IEnumerable<Claim>> BuildSubscriptionClaimsAsync(string userId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId);
        if (subscription is null)
            return [];

        return
        [
            new Claim("plan", subscription.Plan.ToString().ToLowerInvariant()),
            new Claim("planStatus", subscription.Status.ToString().ToLowerInvariant()),
            new Claim("planExpiresAt",
                (subscription.CurrentPeriodEnd ?? subscription.TrialEndsAt)?.ToString("O") ?? "")
        ];
    }
}
