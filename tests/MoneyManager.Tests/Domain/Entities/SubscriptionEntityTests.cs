using Xunit;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Tests.Domain.Entities;

public class SubscriptionEntityTests
{
    [Fact]
    public void Activate_ShouldSetPremiumActiveWithPeriodAndProvider()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.Trial };
        var start = DateTime.UtcNow;
        var end = start.AddMonths(1);

        subscription.Activate("efi", "ext-1", start, end);

        Assert.Equal(PlanType.Premium, subscription.Plan);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("efi", subscription.PaymentProvider);
        Assert.Equal("ext-1", subscription.ExternalSubscriptionId);
        Assert.Equal(start, subscription.CurrentPeriodStart);
        Assert.Equal(end, subscription.CurrentPeriodEnd);
    }

    [Fact]
    public void RenewPeriod_ShouldReactivateAndClearGrace()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.PastDue,
            GraceEndsAt = DateTime.UtcNow.AddDays(2)
        };
        var newEnd = DateTime.UtcNow.AddMonths(1);

        subscription.RenewPeriod(newEnd);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(newEnd, subscription.CurrentPeriodEnd);
        Assert.Null(subscription.GraceEndsAt);
    }

    [Fact]
    public void MarkPastDue_ShouldSetGracePeriod()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.Active };
        var grace = DateTime.UtcNow.AddDays(5);

        subscription.MarkPastDue(grace);

        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
        Assert.Equal(grace, subscription.GraceEndsAt);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledAt()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        subscription.Cancel();

        Assert.Equal(SubscriptionStatus.Cancelled, subscription.Status);
        Assert.NotNull(subscription.CancelledAt);
    }

    [Fact]
    public void MarkExpired_ShouldSetExpiredStatus()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.Active };

        subscription.MarkExpired();

        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Fact]
    public void Downgrade_ShouldSetFreePlanKeepingStatus()
    {
        var subscription = new Subscription { Plan = PlanType.Premium, Status = SubscriptionStatus.Expired };

        subscription.Downgrade();

        Assert.Equal(PlanType.Free, subscription.Plan);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Active, true)]
    [InlineData(SubscriptionStatus.Expired, false)]
    public void IsPremiumActive_ByStatusAlone(SubscriptionStatus status, bool expected)
    {
        var subscription = new Subscription { Status = status };

        Assert.Equal(expected, subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_TrialWithinPeriod_ShouldBeTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_TrialExpired_ShouldBeFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Trial,
            TrialEndsAt = DateTime.UtcNow.AddDays(-1)
        };

        Assert.False(subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_PastDueWithinGrace_ShouldBeTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.PastDue,
            GraceEndsAt = DateTime.UtcNow.AddDays(1)
        };

        Assert.True(subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_PastDueAfterGrace_ShouldBeFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.PastDue,
            GraceEndsAt = DateTime.UtcNow.AddDays(-1)
        };

        Assert.False(subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_CancelledWithinPaidPeriod_ShouldBeTrue()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        Assert.True(subscription.IsPremiumActive());
    }

    [Fact]
    public void IsPremiumActive_CancelledAfterPaidPeriod_ShouldBeFalse()
    {
        var subscription = new Subscription
        {
            Status = SubscriptionStatus.Cancelled,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
        };

        Assert.False(subscription.IsPremiumActive());
    }

    [Fact]
    public void ActivateManually_ShouldSetManualProviderAndAdminMetadata()
    {
        var subscription = new Subscription { Status = SubscriptionStatus.Expired };
        var periodEnd = DateTime.UtcNow.AddDays(30);

        subscription.ActivateManually(periodEnd, "admin1");

        Assert.Equal(PlanType.Premium, subscription.Plan);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal("manual", subscription.PaymentProvider);
        Assert.Equal(periodEnd, subscription.CurrentPeriodEnd);
        Assert.NotNull(subscription.PaymentMetadata);
        Assert.Equal("admin1", subscription.PaymentMetadata["activatedByAdminId"]);
    }

    [Fact]
    public void RevokeManually_ShouldExpireImmediately()
    {
        var subscription = new Subscription
        {
            Plan = PlanType.Premium,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(20)
        };

        subscription.RevokeManually();

        Assert.Equal(PlanType.Free, subscription.Plan);
        Assert.Equal(SubscriptionStatus.Expired, subscription.Status);
        Assert.True(subscription.CurrentPeriodEnd <= DateTime.UtcNow.AddSeconds(1));
        Assert.False(subscription.IsPremiumActive());
    }
}
