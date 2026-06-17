using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IFinancialHealthService
{
    Task<FinancialHealthSettingsResponseDto?> GetSettingsAsync(string userId);
    Task<FinancialHealthSettingsResponseDto> UpsertSettingsAsync(string userId, UpsertFinancialHealthSettingsRequestDto request);
    Task<IEnumerable<PatrimonyBucketResponseDto>> GetBucketsAsync(string userId);
    Task<PatrimonyBucketResponseDto> UpsertBucketAsync(string userId, UpsertPatrimonyBucketRequestDto request);
    Task<SnapshotStatusResponseDto> GetCurrentSnapshotStatusAsync(string userId);
    Task<IEnumerable<MonthlySnapshotResponseDto>> GetSnapshotHistoryAsync(string userId, int year);
    Task ConfirmSnapshotAsync(string userId, int year, int month, ConfirmSnapshotRequestDto request);
    Task DismissSnapshotAsync(string userId, int year, int month);
    Task<HealthScoreResponseDto> GetHealthScoreAsync(string userId);
}

public class FinancialHealthService(IUnitOfWork unitOfWork, IProcessLogger processLogger) : IFinancialHealthService
{
    public async Task<FinancialHealthSettingsResponseDto?> GetSettingsAsync(string userId)
    {
        var settings = await unitOfWork.FinancialHealthSettings.GetByUserIdAsync(userId);
        return settings is null ? null : MapToDto(settings);
    }

    public async Task<FinancialHealthSettingsResponseDto> UpsertSettingsAsync(string userId, UpsertFinancialHealthSettingsRequestDto request)
    {
        processLogger.AddStep("Upsert de configurações de saúde financeira", new Dictionary<string, object?> { ["userId"] = userId });

        var settings = await unitOfWork.FinancialHealthSettings.GetByUserIdAsync(userId);

        if (settings is not null)
        {
            settings.ModeName = request.ModeName;
            settings.InvestPercent = request.InvestPercent;
            settings.ReserveMonths = request.ReserveMonths;
            settings.FireMultiplier = request.FireMultiplier;
            settings.FixedExpensePercent = request.FixedExpensePercent;
            settings.InstallmentPercent = request.InstallmentPercent;
            settings.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.FinancialHealthSettings.UpdateAsync(settings);
        }
        else
        {
            settings = new FinancialHealthSettings
            {
                UserId = userId,
                ModeName = request.ModeName,
                InvestPercent = request.InvestPercent,
                ReserveMonths = request.ReserveMonths,
                FireMultiplier = request.FireMultiplier,
                FixedExpensePercent = request.FixedExpensePercent,
                InstallmentPercent = request.InstallmentPercent
            };
            await unitOfWork.FinancialHealthSettings.AddAsync(settings);
        }

        processLogger.AddStep("Configurações salvas", new Dictionary<string, object?> { ["settingsId"] = settings.Id });
        return MapToDto(settings);
    }

    public async Task<IEnumerable<PatrimonyBucketResponseDto>> GetBucketsAsync(string userId)
    {
        var buckets = await unitOfWork.PatrimonyBuckets.GetByUserIdAsync(userId);
        return buckets.Select(MapToDto);
    }

    public async Task<PatrimonyBucketResponseDto> UpsertBucketAsync(string userId, UpsertPatrimonyBucketRequestDto request)
    {
        processLogger.AddStep("Upsert de patrimônio bucket", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["type"] = request.Type
        });

        var bucket = await unitOfWork.PatrimonyBuckets.GetByUserAndTypeAsync(userId, request.Type);

        if (bucket is not null)
        {
            bucket.InitialBalance = request.InitialBalance;
            bucket.InitialBalanceDate = request.InitialBalanceDate;
            bucket.TrackedCategoryIds = request.TrackedCategoryIds;
            bucket.ExpectedAnnualRate = request.ExpectedAnnualRate;
            bucket.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.PatrimonyBuckets.UpdateAsync(bucket);
        }
        else
        {
            bucket = new PatrimonyBucket
            {
                UserId = userId,
                Type = request.Type,
                InitialBalance = request.InitialBalance,
                InitialBalanceDate = request.InitialBalanceDate,
                TrackedCategoryIds = request.TrackedCategoryIds,
                ExpectedAnnualRate = request.ExpectedAnnualRate
            };
            await unitOfWork.PatrimonyBuckets.AddAsync(bucket);
        }

        processLogger.AddStep("Bucket salvo", new Dictionary<string, object?> { ["bucketId"] = bucket.Id });
        return MapToDto(bucket);
    }

    public async Task<SnapshotStatusResponseDto> GetCurrentSnapshotStatusAsync(string userId)
    {
        var settings = await unitOfWork.FinancialHealthSettings.GetByUserIdAsync(userId);
        var buckets = (await unitOfWork.PatrimonyBuckets.GetByUserIdAsync(userId)).ToList();
        var hasConfiguration = settings is not null && buckets.Count > 0;

        if (!hasConfiguration)
            return new SnapshotStatusResponseDto { HasConfiguration = false, ShowBanner = false };

        var referenceMonth = GetPreviousMonth();
        var snapshots = (await unitOfWork.MonthlySnapshots.GetByUserAndMonthAsync(userId, referenceMonth)).ToList();

        var showBanner = snapshots.Any(s => s.Unconfirmed && !s.DismissedByUser);
        var pendingBuckets = snapshots
            .Where(s => s.Unconfirmed && !s.DismissedByUser)
            .Select(s => new PendingBucketStatusDto
            {
                BucketId = s.BucketId,
                BucketType = buckets.FirstOrDefault(b => b.Id == s.BucketId)?.Type ?? string.Empty,
                EstimatedBalance = s.EstimatedClosingBalance,
                TrackedContributions = s.TrackedContributions,
                EstimatedYield = s.EstimatedYield
            })
            .ToList();

        return new SnapshotStatusResponseDto
        {
            HasConfiguration = true,
            ShowBanner = showBanner,
            ReferenceMonth = referenceMonth,
            PendingBuckets = pendingBuckets
        };
    }

    public async Task<IEnumerable<MonthlySnapshotResponseDto>> GetSnapshotHistoryAsync(string userId, int year)
    {
        var snapshots = await unitOfWork.MonthlySnapshots.GetHistoryByUserAsync(userId, year);
        return snapshots.Select(MapToDto);
    }

    public async Task ConfirmSnapshotAsync(string userId, int year, int month, ConfirmSnapshotRequestDto request)
    {
        var referenceMonth = $"{year:D4}-{month:D2}";

        processLogger.AddStep("Confirmando snapshots do mês", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["referenceMonth"] = referenceMonth
        });

        foreach (var confirmation in request.Buckets)
        {
            var snapshot = await unitOfWork.MonthlySnapshots.GetByBucketAndMonthAsync(confirmation.BucketId, referenceMonth);
            if (snapshot is null)
                throw new KeyNotFoundException($"Snapshot não encontrado para bucket {confirmation.BucketId} no mês {referenceMonth}");

            if (snapshot.UserId != userId)
                throw new UnauthorizedAccessException("Snapshot não pertence ao usuário");

            snapshot.ConfirmedClosingBalance = confirmation.ConfirmedBalance;
            snapshot.Unconfirmed = false;
            snapshot.ConfirmedAt = DateTime.UtcNow;
            snapshot.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.MonthlySnapshots.UpdateAsync(snapshot);
        }

        processLogger.AddStep("Snapshots confirmados", new Dictionary<string, object?> { ["buckets"] = request.Buckets.Count });
    }

    public async Task DismissSnapshotAsync(string userId, int year, int month)
    {
        var referenceMonth = $"{year:D4}-{month:D2}";

        processLogger.AddStep("Descartando snapshots do mês", new Dictionary<string, object?>
        {
            ["userId"] = userId,
            ["referenceMonth"] = referenceMonth
        });

        var snapshots = await unitOfWork.MonthlySnapshots.GetByUserAndMonthAsync(userId, referenceMonth);

        foreach (var snapshot in snapshots)
        {
            snapshot.DismissedByUser = true;
            snapshot.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.MonthlySnapshots.UpdateAsync(snapshot);
        }
    }

    public async Task<HealthScoreResponseDto> GetHealthScoreAsync(string userId)
    {
        processLogger.AddStep("Calculando health score", new Dictionary<string, object?> { ["userId"] = userId });

        var settings = await unitOfWork.FinancialHealthSettings.GetByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Configurações de saúde financeira não encontradas. Configure primeiro.");

        var buckets = (await unitOfWork.PatrimonyBuckets.GetByUserIdAsync(userId)).ToList();

        var allTrackedCategoryIds = buckets
            .SelectMany(b => b.TrackedCategoryIds)
            .Distinct()
            .ToHashSet();

        var now = DateTime.UtcNow;
        var currentMonth = $"{now.Year:D4}-{now.Month:D2}";
        var transactions = (await unitOfWork.Transactions.GetByUserAndMonthAsync(userId, now.Year, now.Month)).ToList();

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        if (totalIncome == 0)
            return new HealthScoreResponseDto { HasData = false, ReferenceMonth = currentMonth };

        var totalInvestments = transactions
            .Where(t => t.CategoryId != null && allTrackedCategoryIds.Contains(t.CategoryId))
            .Sum(t => Math.Abs(t.Amount));

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && (t.CategoryId == null || !allTrackedCategoryIds.Contains(t.CategoryId)))
            .Sum(t => Math.Abs(t.Amount));

        var fireBucket = buckets.FirstOrDefault(b => b.Type == "fire_investment");
        var reserveBucket = buckets.FirstOrDefault(b => b.Type == "emergency_reserve");

        var currentFireBalance = await GetEffectiveBalanceAsync(fireBucket);
        var currentReserveBalance = await GetEffectiveBalanceAsync(reserveBucket);

        var investTarget = totalIncome * (settings.InvestPercent / 100m);
        var investMetric = CalcMetric(totalInvestments, investTarget);

        var reserveTarget = totalExpenses * settings.ReserveMonths;
        var reserveMetric = CalcMetric(currentReserveBalance, reserveTarget);

        var fireTarget = totalIncome * settings.FireMultiplier;
        var fireMetric = CalcMetric(currentFireBalance, fireTarget);

        var expenseTarget = totalIncome * (settings.FixedExpensePercent / 100m);
        var expenseMetric = totalExpenses <= expenseTarget
            ? CalcMetric(expenseTarget, expenseTarget)
            : CalcMetric(expenseTarget, totalExpenses);

        var overallScore = (int)Math.Round(
            (double)(investMetric.ProgressPercent * 0.30m
            + reserveMetric.ProgressPercent * 0.30m
            + fireMetric.ProgressPercent * 0.20m
            + expenseMetric.ProgressPercent * 0.20m));

        var fireInvestmentContributions = transactions
            .Where(t => fireBucket != null && t.CategoryId != null && fireBucket.TrackedCategoryIds.Contains(t.CategoryId))
            .Sum(t => Math.Abs(t.Amount));

        var estimatedMonthsToFire = CalcEstimatedMonthsToFire(
            currentFireBalance, fireTarget, fireInvestmentContributions, fireBucket);

        processLogger.AddStep("Health score calculado", new Dictionary<string, object?> { ["score"] = overallScore });

        return new HealthScoreResponseDto
        {
            HasData = true,
            OverallScore = overallScore,
            ReferenceMonth = currentMonth,
            TotalIncome = totalIncome,
            TotalExpenses = totalExpenses,
            TotalInvestments = totalInvestments,
            InvestmentMetric = investMetric,
            ReserveMetric = reserveMetric,
            FireMetric = fireMetric,
            ExpenseMetric = expenseMetric,
            Projection = new FireProjectionDto
            {
                FireTarget = fireTarget,
                ReserveTarget = reserveTarget,
                CurrentFireBalance = currentFireBalance,
                CurrentReserveBalance = currentReserveBalance,
                EstimatedMonthsToFire = estimatedMonthsToFire
            }
        };
    }

    private async Task<decimal> GetEffectiveBalanceAsync(PatrimonyBucket? bucket)
    {
        if (bucket is null) return 0;

        var currentMonth = $"{DateTime.UtcNow.Year:D4}-{DateTime.UtcNow.Month:D2}";
        var latest = await unitOfWork.MonthlySnapshots.GetLatestConfirmedByBucketAsync(bucket.Id, currentMonth);

        if (latest is null) return bucket.InitialBalance;
        return latest.ConfirmedClosingBalance ?? latest.EstimatedClosingBalance;
    }

    private static MetricScoreDto CalcMetric(decimal current, decimal target)
    {
        if (target == 0) return new MetricScoreDto { ProgressPercent = 100, Status = "on_track" };

        var pct = Math.Min(100m, Math.Round(current / target * 100m, 1));
        return new MetricScoreDto
        {
            CurrentValue = current,
            TargetValue = target,
            ProgressPercent = pct,
            Status = pct >= 80 ? "on_track" : pct >= 50 ? "at_risk" : "off_track"
        };
    }

    private static int? CalcEstimatedMonthsToFire(decimal currentBalance, decimal fireTarget, decimal monthlyContribution, PatrimonyBucket? fireBucket)
    {
        if (fireBucket is null || monthlyContribution <= 0) return null;

        var missing = fireTarget - currentBalance;
        if (missing <= 0) return 0;

        var monthlyRate = Math.Pow(1 + (double)fireBucket.ExpectedAnnualRate, 1.0 / 12) - 1;

        if (monthlyRate <= 0)
            return (int)Math.Ceiling((double)(missing / monthlyContribution));

        // FV = PMT * [(1+r)^n - 1] / r → resolver numericamente
        var pmt = (double)monthlyContribution;
        var target = (double)missing;
        for (var n = 1; n <= 600; n++)
        {
            var fv = pmt * (Math.Pow(1 + monthlyRate, n) - 1) / monthlyRate;
            if (fv >= target) return n;
        }

        return null;
    }

    private static string GetPreviousMonth()
    {
        var now = DateTime.UtcNow;
        var prev = now.AddMonths(-1);
        return $"{prev.Year:D4}-{prev.Month:D2}";
    }

    private static FinancialHealthSettingsResponseDto MapToDto(FinancialHealthSettings s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        ModeName = s.ModeName,
        InvestPercent = s.InvestPercent,
        ReserveMonths = s.ReserveMonths,
        FireMultiplier = s.FireMultiplier,
        FixedExpensePercent = s.FixedExpensePercent,
        InstallmentPercent = s.InstallmentPercent,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static PatrimonyBucketResponseDto MapToDto(PatrimonyBucket b) => new()
    {
        Id = b.Id,
        UserId = b.UserId,
        Type = b.Type,
        InitialBalance = b.InitialBalance,
        InitialBalanceDate = b.InitialBalanceDate,
        TrackedCategoryIds = b.TrackedCategoryIds,
        ExpectedAnnualRate = b.ExpectedAnnualRate,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };

    private static MonthlySnapshotResponseDto MapToDto(MonthlySnapshot s) => new()
    {
        Id = s.Id,
        UserId = s.UserId,
        BucketId = s.BucketId,
        ReferenceMonth = s.ReferenceMonth,
        OpeningBalance = s.OpeningBalance,
        TrackedContributions = s.TrackedContributions,
        EstimatedYield = s.EstimatedYield,
        EstimatedClosingBalance = s.EstimatedClosingBalance,
        ConfirmedClosingBalance = s.ConfirmedClosingBalance,
        EffectiveBalance = s.ConfirmedClosingBalance ?? s.EstimatedClosingBalance,
        TrackedCategoryIds = s.TrackedCategoryIds,
        Unconfirmed = s.Unconfirmed,
        DismissedByUser = s.DismissedByUser,
        ConfirmedAt = s.ConfirmedAt,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
