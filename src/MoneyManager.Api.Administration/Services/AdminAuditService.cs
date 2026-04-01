using MongoDB.Driver;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Api.Administration.Services;

public sealed class AdminAuditService
{
    private readonly IMongoCollection<AdminAuditActionItemDto> _collection;

    public AdminAuditService(MongoContext mongoContext)
    {
        _collection = mongoContext.GetCollection<AdminAuditActionItemDto>("admin_audit_actions");
    }

    public async Task RecordAsync(
        string action,
        string operatorUsername,
        string targetUserId,
        object? parameters,
        bool isSuccess,
        object? result,
        string? errorMessage = null)
    {
        var item = new AdminAuditActionItemDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Action = action,
            OperatorUsername = operatorUsername,
            TargetUserId = targetUserId,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            ParametersJson = parameters is null ? null : System.Text.Json.JsonSerializer.Serialize(parameters),
            ResultJson = result is null ? null : System.Text.Json.JsonSerializer.Serialize(result),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _collection.InsertOneAsync(item);
    }

    public async Task<IReadOnlyList<AdminAuditActionItemDto>> GetRecentAsync(int limit, string? targetUserId, string? action)
    {
        var effectiveLimit = Math.Clamp(limit, 1, 200);

        var filter = Builders<AdminAuditActionItemDto>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(targetUserId))
        {
            filter &= Builders<AdminAuditActionItemDto>.Filter.Eq(x => x.TargetUserId, targetUserId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            filter &= Builders<AdminAuditActionItemDto>.Filter.Eq(x => x.Action, action);
        }

        return await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .Limit(effectiveLimit)
            .ToListAsync();
    }

    public async Task<AdminMonthlyAuditReportDto> GetMonthlyReportAsync(int year, int month)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Month must be between 1 and 12", nameof(month));

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var filter = Builders<AdminAuditActionItemDto>.Filter.And(
            Builders<AdminAuditActionItemDto>.Filter.Gte(x => x.CreatedAtUtc, startDate),
            Builders<AdminAuditActionItemDto>.Filter.Lt(x => x.CreatedAtUtc, endDate)
        );

        var items = await _collection
            .Find(filter)
            .SortByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        // Aggregate data
        var totalActions = items.Count;
        var successfulActions = items.Count(x => x.IsSuccess);
        var failedActions = totalActions - successfulActions;
        var successRate = totalActions > 0 ? Math.Round((decimal)successfulActions / totalActions * 100, 2) : 0m;

        // Count by action type
        var actionCounts = items
            .GroupBy(x => x.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        // Count by operator (as role proxy)
        var roleCounts = items
            .GroupBy(x => x.OperatorUsername)
            .ToDictionary(g => g.Key, g => g.Count());

        var uniqueOperators = items.Select(x => x.OperatorUsername).Distinct().Count();
        var uniqueTargetUsers = items.Select(x => x.TargetUserId).Distinct().Count();

        // Get top 10 recent actions
        var topActions = items.Take(10).ToList();

        return new AdminMonthlyAuditReportDto
        {
            Year = year,
            Month = month,
            TotalActions = totalActions,
            SuccessfulActions = successfulActions,
            FailedActions = failedActions,
            SuccessRate = successRate,
            ActionCounts = actionCounts,
            RoleCounts = roleCounts,
            UniqueOperators = uniqueOperators,
            UniqueTargetUsers = uniqueTargetUsers,
            TopActions = topActions
        };
    }
}
