using MoneyManager.Application.DTOs.Request;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

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

    public RecurringTransactionService(IUnitOfWork unitOfWork, ITransactionService transactionService)
    {
        _unitOfWork = unitOfWork;
        _transactionService = transactionService;
    }

    public async Task<RecurringTransaction> CreateAsync(string userId, CreateRecurringTransactionRequestDto request)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        // For a new schedule we want the first due occurrence to be the StartDate itself.
        // This is important for parcelamento where StartDate = next month on day X.
        var firstOccurrence = request.StartDate;

        var recurring = new RecurringTransaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Type = request.Type,
            Amount = request.Amount,
            Description = request.Description,
            Frequency = request.Frequency,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DayOfMonth = request.DayOfMonth,
            NextOccurrenceDate = firstOccurrence,
            Tags = request.Tags,
            IsActive = true
        };

        await _unitOfWork.RecurringTransactions.AddAsync(recurring);
        await _unitOfWork.SaveChangesAsync();

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
        recurring.StartDate = request.StartDate;
        recurring.EndDate = request.EndDate;
        recurring.DayOfMonth = request.DayOfMonth;
        recurring.Tags = request.Tags;
        recurring.UpdatedAt = DateTime.UtcNow;

        // When updating, reset next occurrence to the start date (or today if start is in the past)
        var resetFrom = request.StartDate.Date < DateTime.UtcNow.Date ? DateTime.UtcNow : request.StartDate;
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
        var recurrences = await _unitOfWork.RecurringTransactions.GetAllAsync();

        var dueRecurrences = recurrences
            .Where(r => r.IsActive
                     && !r.IsDeleted
                     && r.NextOccurrenceDate.Date <= today
                     && (!r.EndDate.HasValue || r.EndDate.Value.Date >= today))
            .ToList();

        foreach (var recurrence in dueRecurrences)
        {
            try
            {
                var transactionRequest = new CreateTransactionRequestDto
                {
                    AccountId = recurrence.AccountId,
                    CategoryId = recurrence.CategoryId,
                    Type = (int)recurrence.Type,
                    Amount = recurrence.Amount,
                    Date = recurrence.NextOccurrenceDate,
                    Description = $"{recurrence.Description} (Recorrente)",
                    Tags = recurrence.Tags,
                    Status = 0
                };

                await _transactionService.CreateAsync(recurrence.UserId, transactionRequest);

                recurrence.LastProcessedDate = recurrence.NextOccurrenceDate;
                recurrence.NextOccurrenceDate = await CalculateNextOccurrence(
                    recurrence.NextOccurrenceDate,
                    recurrence.Frequency,
                    recurrence.DayOfMonth);
                recurrence.UpdatedAt = DateTime.UtcNow;

                if (recurrence.EndDate.HasValue && recurrence.NextOccurrenceDate > recurrence.EndDate.Value)
                {
                    recurrence.IsActive = false;
                }

                await _unitOfWork.RecurringTransactions.UpdateAsync(recurrence);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar recorrência {recurrence.Id}: {ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public Task<DateTime> CalculateNextOccurrence(DateTime currentDate, RecurrenceFrequency frequency, int? dayOfMonth = null)
    {
        DateTime nextDate = frequency switch
        {
            RecurrenceFrequency.Daily => currentDate.AddDays(1),
            RecurrenceFrequency.Weekly => currentDate.AddDays(7),
            RecurrenceFrequency.Biweekly => currentDate.AddDays(14),
            RecurrenceFrequency.Monthly => GetNextMonthlyDate(currentDate, dayOfMonth),
            RecurrenceFrequency.Quarterly => currentDate.AddMonths(3),
            RecurrenceFrequency.Semiannual => currentDate.AddMonths(6),
            RecurrenceFrequency.Annual => currentDate.AddYears(1),
            _ => currentDate.AddMonths(1)
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
