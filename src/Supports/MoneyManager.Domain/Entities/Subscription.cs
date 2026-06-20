using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Domain.Entities;

[BsonIgnoreExtraElements]
public class Subscription
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("plan")]
    public PlanType Plan { get; set; } = PlanType.Free;

    [BsonElement("status")]
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Trial;

    [BsonElement("trialEndsAt")]
    public DateTime? TrialEndsAt { get; set; }

    [BsonElement("currentPeriodStart")]
    public DateTime? CurrentPeriodStart { get; set; }

    [BsonElement("currentPeriodEnd")]
    public DateTime? CurrentPeriodEnd { get; set; }

    [BsonElement("graceEndsAt")]
    public DateTime? GraceEndsAt { get; set; }

    [BsonElement("paymentProvider")]
    public string? PaymentProvider { get; set; }

    // Campo de alto valor de consulta promovido para nível raiz (facilita índice e lookup no webhook).
    [BsonElement("externalSubscriptionId")]
    public string? ExternalSubscriptionId { get; set; }

    // Dados livres por provedor — não cria migration ao trocar/adicionar gateway.
    [BsonElement("paymentMetadata")]
    public Dictionary<string, object>? PaymentMetadata { get; set; }

    [BsonElement("cancelledAt")]
    public DateTime? CancelledAt { get; set; }

    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public void Activate(string provider, string externalSubscriptionId, DateTime periodStart, DateTime periodEnd)
    {
        Plan = PlanType.Premium;
        Status = SubscriptionStatus.Active;
        PaymentProvider = provider;
        ExternalSubscriptionId = externalSubscriptionId;
        CurrentPeriodStart = periodStart;
        CurrentPeriodEnd = periodEnd;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RenewPeriod(DateTime newPeriodEnd)
    {
        Status = SubscriptionStatus.Active;
        CurrentPeriodEnd = newPeriodEnd;
        GraceEndsAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPastDue(DateTime graceEndsAt)
    {
        Status = SubscriptionStatus.PastDue;
        GraceEndsAt = graceEndsAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Downgrade()
    {
        Plan = PlanType.Free;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsPremiumActive() =>
        Status == SubscriptionStatus.Active ||
        (Status == SubscriptionStatus.Trial && TrialEndsAt > DateTime.UtcNow) ||
        (Status == SubscriptionStatus.PastDue && GraceEndsAt > DateTime.UtcNow) ||
        (Status == SubscriptionStatus.Cancelled && CurrentPeriodEnd > DateTime.UtcNow);
}
