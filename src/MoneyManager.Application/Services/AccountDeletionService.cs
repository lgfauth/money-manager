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
        // 1. Verificar se o usuário existe
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("Usuário não encontrado");
        }

        // 2. Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Senha incorreta");
        }

        try
        {
            // 3. Deletar Transações Recorrentes
            var recurringTransactions = await _unitOfWork.RecurringTransactions.GetAllAsync();
            var userRecurring = recurringTransactions.Where(r => r.UserId == userId).ToList();
            foreach (var recurring in userRecurring)
            {
                await _unitOfWork.RecurringTransactions.DeleteAsync(recurring.Id);
            }

            // 4. Deletar Orçamentos
            var budgets = await _unitOfWork.Budgets.GetAllAsync();
            var userBudgets = budgets.Where(b => b.UserId == userId).ToList();
            foreach (var budget in userBudgets)
            {
                await _unitOfWork.Budgets.DeleteAsync(budget.Id);
            }

            // 5. Deletar Transações
            var transactions = await _unitOfWork.Transactions.GetAllAsync();
            var userTransactions = transactions.Where(t => t.UserId == userId).ToList();
            foreach (var transaction in userTransactions)
            {
                await _unitOfWork.Transactions.DeleteAsync(transaction.Id);
            }

            // 6. Deletar Contas
            var accounts = await _unitOfWork.Accounts.GetAllAsync();
            var userAccounts = accounts.Where(a => a.UserId == userId).ToList();
            foreach (var account in userAccounts)
            {
                await _unitOfWork.Accounts.DeleteAsync(account.Id);
            }

            // 7. Deletar Categorias
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var userCategories = categories.Where(c => c.UserId == userId).ToList();
            foreach (var category in userCategories)
            {
                await _unitOfWork.Categories.DeleteAsync(category.Id);
            }

            // 8. Por último, deletar o usuário
            await _unitOfWork.Users.DeleteAsync(userId);

            // 9. Salvar todas as mudanças
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            // Log do erro
            Console.WriteLine($"Erro ao deletar conta do usuário {userId}: {ex.Message}");
            throw new Exception("Erro ao deletar conta. Por favor, tente novamente.", ex);
        }
    }
}
