using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IReportService
{
    Task<SummaryResponseDto> GetMonthlySummaryAsync(string userId, string month);
    Task<IEnumerable<(string CategoryId, decimal Total)>> GetExpensesByCategoryAsync(string userId, string month);
}

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SummaryResponseDto> GetMonthlySummaryAsync(string userId, string month)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var monthTransactions = transactions
            .Where(t => t.UserId == userId &&
                        t.Date.Year.ToString("D4") + "-" + t.Date.Month.ToString("D2") == month &&
                        !t.IsDeleted)
            .ToList();

        var totalIncome = monthTransactions
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.Amount);

        var totalExpense = monthTransactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Amount);

        return new SummaryResponseDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense
        };
    }

    public async Task<IEnumerable<(string CategoryId, decimal Total)>> GetExpensesByCategoryAsync(string userId, string month)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var monthTransactions = transactions
            .Where(t => t.UserId == userId &&
                        t.Date.Year.ToString("D4") + "-" + t.Date.Month.ToString("D2") == month &&
                        t.Type == TransactionType.Expense &&
                        !t.IsDeleted)
            .ToList();

        return monthTransactions
            .GroupBy(t => t.CategoryId ?? "uncategorized")
            .Select(g => (g.Key, g.Sum(t => t.Amount)))
            .ToList();
    }
}
