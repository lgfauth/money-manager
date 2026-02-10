using MoneyManager.Application.DTOs.Request;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Application.Services;

public interface IRecurringTransactionService
{
    Task<RecurringTransaction> CreateAsync(string userId, CreateRecurringTransactionRequestDto request);
    Task<IEnumerable<RecurringTransaction>> GetAllAsync(string userId);
    Task<RecurringTransaction> GetByIdAsync(string userId, string id);
    Task<RecurringTransaction> UpdateAsync(string userId, string id, CreateRecurringTransactionRequestDto request);
    Task DeleteAsync(string userId, string id);
    Task ProcessDueRecurrencesAsync();
    Task<DateTime> CalculateNextOccurrence(DateTime currentDate, RecurrenceFrequency frequency, int? dayOfMonth = null);
}

public class RecurringTransactionService : IRecurringTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionService _transactionService;
    private readonly ILogger<RecurringTransactionService> _logger;

    public RecurringTransactionService(
        IUnitOfWork unitOfWork,
        ITransactionService transactionService,
        ILogger<RecurringTransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _transactionService = transactionService;
        _logger = logger;
    }

    public async Task<RecurringTransaction> CreateAsync(string userId, CreateRecurringTransactionRequestDto request)
    {
        _logger.LogInformation("Creating recurring transaction for user {UserId}, frequency: {Frequency}",
            userId, request.Frequency);

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

        _logger.LogInformation("Recurring transaction {RecurringId} created for user {UserId}, next occurrence: {NextDate}",
            recurring.Id, userId, recurring.NextOccurrenceDate);

        return recurring;
    }

    public async Task<IEnumerable<RecurringTransaction>> GetAllAsync(string userId)
    {
        var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();
        return recurrences.Where(r => r.UserId == userId && !r.IsDeleted);
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

    public async Task ProcessDueRecurrencesAsync()
    {
        var today = DateTime.UtcNow.Date;
        _logger.LogInformation("Starting recurring transactions processing for date: {Date}", today);

        var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();

        var dueRecurrences = recurrences
            .Where(r => r.IsActive
                     && !r.IsDeleted
                     && r.NextOccurrenceDate.Date <= today
                     && (!r.EndDate.HasValue || r.EndDate.Value.Date >= today))
            .ToList();

        _logger.LogInformation("Found {Count} due recurring transactions to process", dueRecurrences.Count);

        foreach (var recurrence in dueRecurrences)
        {
            try
            {
                _logger.LogDebug("Processing recurring transaction {RecurringId} for user {UserId}",
                    recurrence.Id, recurrence.UserId);

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

                    _logger.LogDebug("Transaction created from recurring {RecurringId} for date: {Date}",
                        recurrence.Id, recurrence.NextOccurrenceDate);

                    recurrence.LastProcessedDate = recurrence.NextOccurrenceDate;
                    recurrence.NextOccurrenceDate = await CalculateNextOccurrence(
                        recurrence.NextOccurrenceDate,
                        recurrence.Frequency,
                        recurrence.DayOfMonth);
                    recurrence.UpdatedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Processed {Count} transaction(s) from recurring {RecurringId}, next occurrence: {NextDate}",
                    processedCount, recurrence.Id, recurrence.NextOccurrenceDate);

                if (recurrence.EndDate.HasValue && recurrence.NextOccurrenceDate > recurrence.EndDate.Value)
                {
                    recurrence.IsActive = false;
                    _logger.LogInformation("Recurring transaction {RecurringId} reached end date and was deactivated",
                        recurrence.Id);
                }

                await _unitOfWork.RecurringTransactions.UpdateAsync(recurrence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring transaction {RecurringId} for user {UserId}",
                    recurrence.Id, recurrence.UserId);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Recurring transactions processing completed");
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
