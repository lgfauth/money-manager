using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IAccountDeletionService
{
    Task<bool> DeleteUserAccountAsync(string userId, string password);
    Task<int> GetUserDataCountAsync(string userId);
}

public class AccountDeletionService : IAccountDeletionService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountDeletionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> GetUserDataCountAsync(string userId)
    {
        var accounts = await _unitOfWork.Accounts.GetAllAsync();
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        var budgets = await _unitOfWork.Budgets.GetAllAsync();
        var recurring = await _unitOfWork.RecurringTransactions.GetAllAsync();

        var totalCount =
            accounts.Count(a => a.UserId == userId) +
            categories.Count(c => c.UserId == userId) +
            transactions.Count(t => t.UserId == userId) +
            budgets.Count(b => b.UserId == userId) +
            recurring.Count(r => r.UserId == userId);

        return totalCount;
    }

    public async Task<bool> DeleteUserAccountAsync(string userId, string password)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Senha incorreta");
        }

        try
        {
            var recurringTransactions = await _unitOfWork.RecurringTransactions.GetAllAsync();
            var userRecurring = recurringTransactions.Where(r => r.UserId == userId).ToList();
            foreach (var recurring in userRecurring)
            {
                await _unitOfWork.RecurringTransactions.DeleteAsync(recurring.Id);
            }

            var budgets = await _unitOfWork.Budgets.GetAllAsync();
            var userBudgets = budgets.Where(b => b.UserId == userId).ToList();
            foreach (var budget in userBudgets)
            {
                await _unitOfWork.Budgets.DeleteAsync(budget.Id);
            }

            var transactions = await _unitOfWork.Transactions.GetAllAsync();
            var userTransactions = transactions.Where(t => t.UserId == userId).ToList();
            foreach (var transaction in userTransactions)
            {
                await _unitOfWork.Transactions.DeleteAsync(transaction.Id);
            }

            var accounts = await _unitOfWork.Accounts.GetAllAsync();
            var userAccounts = accounts.Where(a => a.UserId == userId).ToList();
            foreach (var account in userAccounts)
            {
                await _unitOfWork.Accounts.DeleteAsync(account.Id);
            }

            var categories = await _unitOfWork.Categories.GetAllAsync();
            var userCategories = categories.Where(c => c.UserId == userId).ToList();
            foreach (var category in userCategories)
            {
                await _unitOfWork.Categories.DeleteAsync(category.Id);
            }

            await _unitOfWork.Users.DeleteAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao deletar conta do usuário {userId}: {ex.Message}");
            throw new Exception("Erro ao deletar conta. Por favor, tente novamente.", ex);
        }
    }
}

