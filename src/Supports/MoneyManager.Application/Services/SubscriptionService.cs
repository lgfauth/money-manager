using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> ActivateTrialAsync(string userId);
    Task<CreateSubscriptionResponseDto> CreateAsync(string userId, CreateSubscriptionRequestDto request);
    Task HandlePaymentWebhookAsync(string rawPayload, IDictionary<string, string> headers);
    Task CancelAsync(string userId);
    Task<SubscriptionResponseDto> GetByUserIdAsync(string userId);

    // Reutilizado por outros services premium — único ponto de verdade para "o usuário tem acesso premium?".
    Task EnsurePremiumAccessAsync(string userId);

    // Admin — controle manual de premium sem gateway de pagamento.
    Task<IReadOnlyList<AdminUserSubscriptionResponseDto>> GetAllForAdminAsync(int page, int pageSize);
    Task<AdminUserSubscriptionResponseDto> ActivatePremiumManuallyAsync(string userId, int durationDays, string adminUserId);
    Task<AdminUserSubscriptionResponseDto> RevokePremiumManuallyAsync(string userId);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IProcessLogger _processLogger;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        IProcessLogger processLogger,
        ILogger<SubscriptionService> logger)
    {
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _processLogger = processLogger;
        _logger = logger;
    }

    public async Task<SubscriptionResponseDto> ActivateTrialAsync(string userId)
    {
        var existing = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId);
        if (existing is not null)
            throw new InvalidOperationException("Usuário já possui uma assinatura");

        var subscription = new Subscription
        {
            UserId = userId,
            Plan = PlanType.Free,
            Status = SubscriptionStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(14)
        };

        await _unitOfWork.Subscriptions.AddAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Trial ativado para usuário {UserId}", userId);
        return MapToDto(subscription);
    }

    public async Task<CreateSubscriptionResponseDto> CreateAsync(string userId, CreateSubscriptionRequestDto request)
    {
        _processLogger.AddStep("Iniciando criação de assinatura premium", new Dictionary<string, object?> { ["userId"] = userId });

        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Assinatura não encontrada para o usuário");

        if (subscription.Status == SubscriptionStatus.Active)
            throw new InvalidOperationException("Assinatura já está ativa");

        var gatewayResult = await _paymentGateway.CreateSubscriptionAsync(new CreateSubscriptionGatewayRequest
        {
            UserId = userId,
            PayerName = request.PayerName,
            PayerCpf = request.PayerCpf,
            PayerEmail = request.PayerEmail
        });

        // Estado permanece Trial até o webhook confirmar o pagamento.
        subscription.PaymentProvider = _paymentGateway.ProviderName;
        subscription.ExternalSubscriptionId = gatewayResult.ExternalSubscriptionId;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Subscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Assinatura criada no gateway", new Dictionary<string, object?>
        {
            ["externalSubscriptionId"] = gatewayResult.ExternalSubscriptionId
        });

        return new CreateSubscriptionResponseDto
        {
            PaymentUrl = gatewayResult.PaymentUrl,
            ExternalSubscriptionId = gatewayResult.ExternalSubscriptionId
        };
    }

    public async Task HandlePaymentWebhookAsync(string rawPayload, IDictionary<string, string> headers)
    {
        var validation = await _paymentGateway.ValidateAndParseWebhookAsync(rawPayload, headers);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Webhook de pagamento inválido recebido, ignorando");
            return; // controller responde 200 mesmo assim — evita retry storm do provedor
        }

        var subscription = await _unitOfWork.Subscriptions.GetByExternalSubscriptionIdAsync(validation.ExternalSubscriptionId);
        if (subscription is null)
        {
            _logger.LogWarning("Webhook referencia assinatura desconhecida {ExternalSubscriptionId}", validation.ExternalSubscriptionId);
            return;
        }

        switch (validation.EventType)
        {
            case WebhookEventType.PaymentConfirmed when subscription.Status == SubscriptionStatus.Trial:
                subscription.Activate(_paymentGateway.ProviderName, validation.ExternalSubscriptionId,
                    validation.PeriodStart ?? DateTime.UtcNow, validation.PeriodEnd ?? DateTime.UtcNow.AddMonths(1));
                break;
            case WebhookEventType.PaymentConfirmed when subscription.Status == SubscriptionStatus.PastDue:
                subscription.RenewPeriod(validation.PeriodEnd ?? DateTime.UtcNow.AddMonths(1));
                break;
            case WebhookEventType.PaymentFailed:
                subscription.MarkPastDue(DateTime.UtcNow.AddDays(5));
                break;
            case WebhookEventType.SubscriptionCancelled:
                subscription.Cancel();
                break;
        }

        await _unitOfWork.Subscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Webhook de pagamento processado", new Dictionary<string, object?>
        {
            ["eventType"] = validation.EventType.ToString(),
            ["subscriptionId"] = subscription.Id
        });
    }

    public async Task CancelAsync(string userId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Assinatura não encontrada");

        if (subscription.ExternalSubscriptionId is not null)
            await _paymentGateway.CancelSubscriptionAsync(subscription.ExternalSubscriptionId);

        subscription.Cancel();
        await _unitOfWork.Subscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<SubscriptionResponseDto> GetByUserIdAsync(string userId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Assinatura não encontrada");

        return MapToDto(subscription);
    }

    public async Task EnsurePremiumAccessAsync(string userId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId);
        if (subscription is null || !subscription.IsPremiumActive())
            throw new PremiumRequiredException();
    }

    public async Task<IReadOnlyList<AdminUserSubscriptionResponseDto>> GetAllForAdminAsync(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        var subscriptions = await _unitOfWork.Subscriptions.GetAllAsync(skip, pageSize);

        var result = new List<AdminUserSubscriptionResponseDto>();
        foreach (var subscription in subscriptions)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(subscription.UserId);
            if (user is null || user.IsDeleted)
                continue;

            result.Add(MapToAdminDto(subscription, user));
        }

        return result;
    }

    public async Task<AdminUserSubscriptionResponseDto> ActivatePremiumManuallyAsync(string userId, int durationDays, string adminUserId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId);

        if (subscription is null)
        {
            subscription = new Subscription { UserId = userId };
            subscription.ActivateManually(DateTime.UtcNow.AddDays(durationDays), adminUserId);
            await _unitOfWork.Subscriptions.AddAsync(subscription);
        }
        else
        {
            subscription.ActivateManually(DateTime.UtcNow.AddDays(durationDays), adminUserId);
            await _unitOfWork.Subscriptions.UpdateAsync(subscription);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Premium ativado manualmente para usuário {UserId} por {AdminUserId}, duração {DurationDays} dias",
            userId, adminUserId, durationDays);

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        return MapToAdminDto(subscription, user);
    }

    public async Task<AdminUserSubscriptionResponseDto> RevokePremiumManuallyAsync(string userId)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Assinatura não encontrada para o usuário");

        subscription.RevokeManually();
        await _unitOfWork.Subscriptions.UpdateAsync(subscription);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Premium revogado manualmente para usuário {UserId}", userId);

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException("Usuário não encontrado");

        return MapToAdminDto(subscription, user);
    }

    private static SubscriptionResponseDto MapToDto(Subscription s) => new()
    {
        Id = s.Id,
        Plan = s.Plan.ToString(),
        Status = s.Status.ToString(),
        TrialEndsAt = s.TrialEndsAt,
        CurrentPeriodEnd = s.CurrentPeriodEnd,
        IsPremiumActive = s.IsPremiumActive()
    };

    private static AdminUserSubscriptionResponseDto MapToAdminDto(Subscription s, User u) => new()
    {
        UserId = u.Id,
        Name = u.Name,
        Email = u.Email,
        Plan = s.Plan.ToString(),
        Status = s.Status.ToString(),
        IsPremiumActive = s.IsPremiumActive(),
        TrialEndsAt = s.TrialEndsAt,
        CurrentPeriodEnd = s.CurrentPeriodEnd,
        PaymentProvider = s.PaymentProvider,
        UserCreatedAt = u.CreatedAt
    };
}
