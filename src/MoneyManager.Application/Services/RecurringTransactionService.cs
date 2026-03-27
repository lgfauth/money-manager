using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface IRecurringTransactionService
{
    Task<RecurringTransaction> CreateAsync(string userId, CreateRecurringTransactionRequestDto request);
    Task<IEnumerable<RecurringTransaction>> GetAllAsync(string userId);
    Task<RecurringTransaction> GetByIdAsync(string userId, string id);
    Task<RecurringTransaction> UpdateAsync(string userId, string id, CreateRecurringTransactionRequestDto request);
    Task DeleteAsync(string userId, string id);
    Task<RecurringProcessingSummary> ProcessDueRecurrencesAsync();
    Task<DateTime> CalculateNextOccurrence(DateTime currentDate, RecurrenceFrequency frequency, int? dayOfMonth = null);
}

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionService _transactionService;
    private readonly IProcessLogger _processLogger;

    public RecurringTransactionService(
        IUnitOfWork unitOfWork,
        ITransactionService transactionService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _transactionService = transactionService;
        _processLogger = processLogger;
    }

    public async Task<RecurringTransaction> CreateAsync(string userId, CreateRecurringTransactionRequestDto request)
    {
        _processLogger.AddStep("Creating recurring transaction", new Dictionary<string, object?> { ["frequency"] = request.Frequency.ToString() });

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        // Normalize dates to remove time component (only date matters for scheduling)
        // For a new schedule, first due occurrence = StartDate (date only, no time)
        var firstOccurrence = request.StartDate.Date;

        var recurring = new RecurringTransaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description,
            Frequency = request.Frequency,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate?.Date,
            DayOfMonth = request.DayOfMonth,
            NextOccurrenceDate = firstOccurrence,
            Tags = request.Tags,
            IsActive = true
        };

        await _unitOfWork.RecurringTransactions.AddAsync(recurring);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Recurring transaction created", new Dictionary<string, object?> { ["recurringId"] = recurring.Id, ["nextOccurrence"] = recurring.NextOccurrenceDate.ToString("O") });

        return recurring;
    }

    public async Task<IEnumerable<RecurringTransaction>> GetAllAsync(string userId)
    {
        try
        {
            var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();

            // Defensive filtering: legacy/malformed records should not break the whole page.
            return recurrences
                .Where(r => r is not null)
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            _processLogger.AddError("Failed to load recurring transactions, returning empty list", ex);

            return [];
        }
    }

    public async Task<RecurringTransaction> GetByIdAsync(string userId, string id)
    {
        var recurring = await _unitOfWork.RecurringTransactions.GetByIdAsync(id);
        if (recurring == null || recurring.UserId != userId || recurring.IsDeleted)
            throw new KeyNotFoundException("Recurring transaction not found");

        return recurring;
    }

    public async Task<RecurringTransaction> UpdateAsync(string userId, string id, CreateRecurringTransactionRequestDto request)
    {
        var recurring = await _unitOfWork.RecurringTransactions.GetByIdAsync(id);
        if (recurring == null || recurring.UserId != userId || recurring.IsDeleted)
            throw new KeyNotFoundException("Recurring transaction not found");

        recurring.AccountId = request.AccountId;
        recurring.CategoryId = request.CategoryId;
        recurring.Type = request.Type;
        recurring.Amount = request.Amount;
        recurring.Description = request.Description;
        recurring.Frequency = request.Frequency;
        recurring.StartDate = request.StartDate.Date;
        recurring.EndDate = request.EndDate?.Date;
        recurring.DayOfMonth = request.DayOfMonth;
        recurring.Tags = request.Tags;
        recurring.UpdatedAt = DateTime.UtcNow;

        // When updating, reset next occurrence to the start date (or today if start is in the past)
        var resetFrom = request.StartDate.Date < DateTime.UtcNow.Date ? DateTime.UtcNow.Date : request.StartDate.Date;
        recurring.NextOccurrenceDate = resetFrom;

        await _unitOfWork.RecurringTransactions.UpdateAsync(recurring);
        await _unitOfWork.SaveChangesAsync();

        return recurring;
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var recurring = await _unitOfWork.RecurringTransactions.GetByIdAsync(id);
        if (recurring == null || recurring.UserId != userId || recurring.IsDeleted)
            throw new KeyNotFoundException("Recurring transaction not found");

        recurring.IsDeleted = true;
        recurring.IsActive = false;
        recurring.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.RecurringTransactions.UpdateAsync(recurring);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<RecurringProcessingSummary> ProcessDueRecurrencesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var summary = new RecurringProcessingSummary();
        _processLogger.AddStep("Starting recurring transactions processing", new Dictionary<string, object?> { ["date"] = today.ToString("O") });

        var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();

        var dueRecurrences = recurrences
            .Where(r => r.IsActive
                     && !r.IsDeleted
                     && r.NextOccurrenceDate.Date <= today
                     && (!r.EndDate.HasValue || r.EndDate.Value.Date >= today))
            .ToList();

        _processLogger.AddStep("Due recurring transactions found", new Dictionary<string, object?> { ["count"] = dueRecurrences.Count });

        foreach (var recurrence in dueRecurrences)
        {
            try
            {
                _processLogger.AddStep("Processing recurring transaction", new Dictionary<string, object?> { ["recurringId"] = recurrence.Id });

                // Process all overdue occurrences (backlog support)
                var processedCount = 0;
                while (recurrence.NextOccurrenceDate.Date <= today 
                       && (!recurrence.EndDate.HasValue || recurrence.NextOccurrenceDate.Date <= recurrence.EndDate.Value.Date))
                {
                    var transactionRequest = new CreateTransactionRequestDto
                    {
                        AccountId = recurrence.AccountId,
                        CategoryId = recurrence.CategoryId,
                        Type = (int)recurrence.Type,
                        Amount = recurrence.Amount,
                        Date = recurrence.NextOccurrenceDate.Date, // Ensure transaction date has no time component
                        Description = $"{recurrence.Description} (Recorrente)",
                        Tags = recurrence.Tags,
                        Status = 0
                    };

                    await _transactionService.CreateAsync(recurrence.UserId, transactionRequest);
                    processedCount++;

                    _processLogger.AddStep("Transaction created from recurring", new Dictionary<string, object?> { ["recurringId"] = recurrence.Id, ["date"] = recurrence.NextOccurrenceDate.ToString("O") });

                    recurrence.LastProcessedDate = recurrence.NextOccurrenceDate;
                    recurrence.NextOccurrenceDate = await CalculateNextOccurrence(
                        recurrence.NextOccurrenceDate,
                        recurrence.Frequency,
                        recurrence.DayOfMonth);
                    recurrence.UpdatedAt = DateTime.UtcNow;
                }

                _processLogger.AddStep("Recurring batch processed", new Dictionary<string, object?> { ["recurringId"] = recurrence.Id, ["processedCount"] = processedCount, ["nextOccurrence"] = recurrence.NextOccurrenceDate.ToString("O") });

                if (processedCount > 0)
                    summary.Add(recurrence.UserId, processedCount);

                if (recurrence.EndDate.HasValue && recurrence.NextOccurrenceDate > recurrence.EndDate.Value)
                {
                    recurrence.IsActive = false;
                    _processLogger.AddStep("Recurring transaction deactivated (end date reached)", new Dictionary<string, object?> { ["recurringId"] = recurrence.Id });
                }

                await _unitOfWork.RecurringTransactions.UpdateAsync(recurrence);
            }
            catch (Exception ex)
            {
                _processLogger.AddError($"Error processing recurring transaction {recurrence.Id}", ex);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _processLogger.AddStep("Recurring transactions processing completed");
        return summary;
    }

    public Task<DateTime> CalculateNextOccurrence(DateTime currentDate, RecurrenceFrequency frequency, int? dayOfMonth = null)
    {
        // Always work with dates only (no time component)
        var current = currentDate.Date;

        DateTime nextDate = frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(1),
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Biweekly => current.AddDays(14),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(current, dayOfMonth),
            RecurrenceFrequency.Quarterly => current.AddMonths(3),
            RecurrenceFrequency.Semiannual => current.AddMonths(6),
            RecurrenceFrequency.Annual => current.AddYears(1),
            _ => current.AddMonths(1)
        };

        return Task.FromResult(nextDate);
    }

    private static DateTime GetNextMonthlyDate(DateTime currentDate, int? dayOfMonth)
    {
        if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
        {
            return currentDate.AddMonths(1);
        }

        var nextMonth = currentDate.AddMonths(1);
        var daysInNextMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
        var day = Math.Min(dayOfMonth.Value, daysInNextMonth);

        return new DateTime(nextMonth.Year, nextMonth.Month, day);
    }
}
