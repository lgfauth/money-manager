using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IBudgetService
{
    Task<BudgetResponseDto> CreateOrUpdateAsync(string userId, string month, List<BudgetItemRequestDto> items);
    Task<BudgetResponseDto> GetByMonthAsync(string userId, string month);
    Task<IEnumerable<BudgetResponseDto>> GetAllAsync(string userId);
}

public class BudgetService : IBudgetService
{
    private readonly IUnitOfWork _unitOfWork;

    public BudgetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetResponseDto> CreateOrUpdateAsync(string userId, string month, List<BudgetItemRequestDto> items)
    {
        var budgets = await _unitOfWork.Budgets.GetAllAsync();
        var budget = budgets.FirstOrDefault(b => b.UserId == userId && b.Month == month);

        if (budget == null)
        {
            budget = new Budget
            {
                UserId = userId,
                Month = month,
                Items = items.Select(i => new BudgetItem { CategoryId = i.CategoryId, LimitAmount = i.LimitAmount }).ToList()
            };
            await _unitOfWork.Budgets.AddAsync(budget);
        }
        else
        {
            budget.Items = items.Select(i => new BudgetItem { CategoryId = i.CategoryId, LimitAmount = i.LimitAmount }).ToList();
            budget.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Budgets.UpdateAsync(budget);
        }

        await UpdateSpentAmountsAsync(budget);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(budget);
    }

    public async Task<BudgetResponseDto> GetByMonthAsync(string userId, string month)
    {
        var budgets = await _unitOfWork.Budgets.GetAllAsync();
        var budget = budgets.FirstOrDefault(b => b.UserId == userId && b.Month == month);

        if (budget == null)
            throw new KeyNotFoundException("Budget not found");

        await UpdateSpentAmountsAsync(budget);
        return MapToDto(budget);
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetAllAsync(string userId)
    {
        var budgets = await _unitOfWork.Budgets.GetAllAsync();
        var userBudgets = budgets.Where(b => b.UserId == userId).ToList();

        foreach (var budget in userBudgets)
        {
            await UpdateSpentAmountsAsync(budget);
        }

        return userBudgets.Select(MapToDto);
    }

    private async Task UpdateSpentAmountsAsync(Budget budget)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var monthTransactions = transactions
            .Where(t => t.UserId == budget.UserId && t.Date.Year.ToString("D4") + "-" + t.Date.Month.ToString("D2") == budget.Month && t.Type == TransactionType.Expense && !t.IsDeleted)
            .ToList();

        foreach (var item in budget.Items)
        {
            item.SpentAmount = monthTransactions
                .Where(t => t.CategoryId == item.CategoryId)
                .Sum(t => t.Amount);
        }
    }

    private static BudgetResponseDto MapToDto(Budget budget)
    {
        return new BudgetResponseDto
        {
            Id = budget.Id,
            Month = budget.Month,
            Items = budget.Items.Select(i => new BudgetItemResponseDto
            {
                CategoryId = i.CategoryId,
                LimitAmount = i.LimitAmount,
                SpentAmount = i.SpentAmount
            }).ToList(),
            CreatedAt = budget.CreatedAt
        };
    }
}
