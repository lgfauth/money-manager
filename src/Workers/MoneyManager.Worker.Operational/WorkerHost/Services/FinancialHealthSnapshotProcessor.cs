using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace TransactionSchedulerWorker.WorkerHost.Services;

internal sealed class FinancialHealthSnapshotProcessor(
    IProcessLogger processLogger,
    IUnitOfWork unitOfWork)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var referenceMonth = GetPreviousMonth();

        processLogger.AddStep("Iniciando geração de snapshots mensais de saúde financeira", new Dictionary<string, object?>
        {
            ["referenceMonth"] = referenceMonth
        });

        var allBuckets = (await unitOfWork.PatrimonyBuckets.GetAllUsersWithBucketsAsync()).ToList();
        var grouped = allBuckets.GroupBy(b => b.UserId);

        var totalCreated = 0;
        var totalSkipped = 0;

        foreach (var userBuckets in grouped)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var bucket in userBuckets)
            {
                var existing = await unitOfWork.MonthlySnapshots.GetByBucketAndMonthAsync(bucket.Id, referenceMonth);
                if (existing != null)
                {
                    totalSkipped++;
                    continue;
                }

                var openingBalance = await GetOpeningBalanceAsync(bucket, referenceMonth);
                var trackedContributions = await GetTrackedContributionsAsync(bucket, referenceMonth);

                var monthlyRate = Math.Pow(1 + (double)bucket.ExpectedAnnualRate, 1.0 / 12) - 1;
                var estimatedYield = openingBalance * (decimal)monthlyRate;

                var snapshot = new MonthlySnapshot
                {
                    UserId = bucket.UserId,
                    BucketId = bucket.Id,
                    ReferenceMonth = referenceMonth,
                    OpeningBalance = openingBalance,
                    TrackedContributions = trackedContributions,
                    EstimatedYield = estimatedYield,
                    EstimatedClosingBalance = openingBalance + trackedContributions + estimatedYield,
                    TrackedCategoryIds = [.. bucket.TrackedCategoryIds],
                    Unconfirmed = true,
                    DismissedByUser = false
                };

                await unitOfWork.MonthlySnapshots.AddAsync(snapshot);
                totalCreated++;
            }
        }

        processLogger.AddStep("Snapshots de saúde financeira gerados", new Dictionary<string, object?>
        {
            ["criados"] = totalCreated,
            ["ignorados"] = totalSkipped
        });
    }

    private async Task<decimal> GetOpeningBalanceAsync(PatrimonyBucket bucket, string referenceMonth)
    {
        var previous = await unitOfWork.MonthlySnapshots.GetLatestConfirmedByBucketAsync(bucket.Id, referenceMonth);
        if (previous != null)
            return previous.ConfirmedClosingBalance ?? previous.EstimatedClosingBalance;

        return bucket.InitialBalance;
    }

    private async Task<decimal> GetTrackedContributionsAsync(PatrimonyBucket bucket, string referenceMonth)
    {
        if (bucket.TrackedCategoryIds.Count == 0) return 0;

        var parts = referenceMonth.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        var firstDayOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddTicks(-1);
        var startDate = bucket.InitialBalanceDate > firstDayOfMonth ? bucket.InitialBalanceDate : firstDayOfMonth;

        var transactions = await unitOfWork.Transactions.GetByUserAndMonthAsync(bucket.UserId, year, month);

        return transactions
            .Where(t => t.CategoryId != null
                && bucket.TrackedCategoryIds.Contains(t.CategoryId)
                && t.Date >= startDate
                && t.Date <= lastDayOfMonth)
            .Sum(t => Math.Abs(t.Amount));
    }

    private static string GetPreviousMonth()
    {
        var prev = DateTime.UtcNow.AddMonths(-1);
        return $"{prev.Year:D4}-{prev.Month:D2}";
    }
}
