using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Seeds;

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

    /// <summary>Sugestões de categorias do onboarding: a mesma lista padrão do botão "Criar categorias padrão".</summary>
    public Task<List<CategorySuggestionDto>> GetCategorySuggestionsAsync()
    {
        var suggestions = DefaultCategorySeed.Categories
            .Select(c => new CategorySuggestionDto
            {
                Name = c.Name,
                Type = c.Type,
                Color = c.Color,
                IsDefault = true
            })
            .ToList();

        return Task.FromResult(suggestions);
    }

    public async Task CompleteOnboardingAsync(string userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("Usuário não encontrado");
        
        await Task.CompletedTask;
    }
}
