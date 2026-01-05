using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Enums;

namespace MoneyManager.Application.Services;

public interface IOnboardingService
{
    Task<OnboardingStatusDto> GetOnboardingStatusAsync(string userId);
    Task<List<CategorySuggestionDto>> GetCategorySuggestionsAsync();
    Task CompleteOnboardingAsync(string userId);
}

public class OnboardingService : IOnboardingService
{
    private readonly IUnitOfWork _unitOfWork;

    public OnboardingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OnboardingStatusDto> GetOnboardingStatusAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("Usuário não encontrado");

        var accounts = await _unitOfWork.Accounts.GetAllAsync();
        var userAccounts = accounts.Where(a => a.UserId == userId && !a.IsDeleted).ToList();

        var categories = await _unitOfWork.Categories.GetAllAsync();
        var userCategories = categories.Where(c => c.UserId == userId && !c.IsDeleted).ToList();

        var budgets = await _unitOfWork.Budgets.GetAllAsync();
        var userBudgets = budgets.Where(b => b.UserId == userId).ToList();

        var recurring = await _unitOfWork.RecurringTransactions.GetAllAsync();
        var userRecurring = recurring.Where(r => r.UserId == userId && !r.IsDeleted).ToList();

        var hasAccounts = userAccounts.Any();
        var hasCategories = userCategories.Any();
        var hasBudget = userBudgets.Any();
        var hasRecurring = userRecurring.Any();

        var completedSteps = 0;
        var totalSteps = 4;

        var pendingSteps = new List<string>();

        if (hasAccounts) completedSteps++;
        else pendingSteps.Add("Criar contas");

        if (hasCategories) completedSteps++;
        else pendingSteps.Add("Criar categorias");

        if (hasBudget) completedSteps++;
        else pendingSteps.Add("Definir orçamento");

        if (hasRecurring) completedSteps++;
        else pendingSteps.Add("Configurar despesas recorrentes");

        var percentage = (int)((completedSteps / (double)totalSteps) * 100);
        var isCompleted = percentage == 100;

        return new OnboardingStatusDto
        {
            IsCompleted = isCompleted,
            HasAccounts = hasAccounts,
            HasCategories = hasCategories,
            HasBudget = hasBudget,
            HasRecurringTransactions = hasRecurring,
            CompletionPercentage = percentage,
            PendingSteps = pendingSteps
        };
    }

    public async Task<List<CategorySuggestionDto>> GetCategorySuggestionsAsync()
    {
        return await Task.FromResult(new List<CategorySuggestionDto>
        {
            // Receitas
            new CategorySuggestionDto { Name = "Salário", Type = (int)CategoryType.Income, Color = "#28a745", Icon = "fa-money-bill-wave", IsDefault = true },
            new CategorySuggestionDto { Name = "Freelance", Type = (int)CategoryType.Income, Color = "#17a2b8", Icon = "fa-laptop-code", IsDefault = true },
            new CategorySuggestionDto { Name = "Investimentos", Type = (int)CategoryType.Income, Color = "#20c997", Icon = "fa-chart-line", IsDefault = true },
            
            // Despesas
            new CategorySuggestionDto { Name = "Moradia", Type = (int)CategoryType.Expense, Color = "#fd7e14", Icon = "fa-home", IsDefault = true },
            new CategorySuggestionDto { Name = "Alimentação", Type = (int)CategoryType.Expense, Color = "#dc3545", Icon = "fa-utensils", IsDefault = true },
            new CategorySuggestionDto { Name = "Transporte", Type = (int)CategoryType.Expense, Color = "#ffc107", Icon = "fa-car", IsDefault = true },
            new CategorySuggestionDto { Name = "Lazer", Type = (int)CategoryType.Expense, Color = "#007bff", Icon = "fa-gamepad", IsDefault = true },
        });
    }

    public async Task CompleteOnboardingAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("Usuário não encontrado");
        
        await Task.CompletedTask;
    }
}
