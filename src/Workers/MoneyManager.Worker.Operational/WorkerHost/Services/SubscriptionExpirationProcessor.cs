using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class SubscriptionExpirationProcessor(
    IProcessLogger processLogger,
    IUnitOfWork unitOfWork)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        processLogger.AddStep("Iniciando expiração de assinaturas");

        var now = DateTime.UtcNow;

        var pastDueExpired = await unitOfWork.Subscriptions.GetExpiringAsync(SubscriptionStatus.PastDue, now);
        foreach (var subscription in pastDueExpired)
        {
            subscription.MarkExpired();
            subscription.Downgrade();
            await unitOfWork.Subscriptions.UpdateAsync(subscription);
        }

        var trialExpired = await unitOfWork.Subscriptions.GetExpiringAsync(SubscriptionStatus.Trial, now);
        foreach (var subscription in trialExpired)
        {
            subscription.Downgrade();
            await unitOfWork.Subscriptions.UpdateAsync(subscription);
        }

        var cancelledExpired = await unitOfWork.Subscriptions.GetExpiringAsync(SubscriptionStatus.Cancelled, now);
        foreach (var subscription in cancelledExpired)
        {
            subscription.Downgrade();
            await unitOfWork.Subscriptions.UpdateAsync(subscription);
        }

        await unitOfWork.SaveChangesAsync();

        processLogger.AddStep("Expiração de assinaturas finalizada", new Dictionary<string, object?>
        {
            ["pastDueExpired"] = pastDueExpired.Count(),
            ["trialExpired"] = trialExpired.Count(),
            ["cancelledExpired"] = cancelledExpired.Count()
        });
    }
}
