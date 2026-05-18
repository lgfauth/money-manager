using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Application.Services;

public interface IAccountDeletionService
{
    Task<bool> DeleteUserAccountAsync(string userId, string password);
    Task<int> GetUserDataCountAsync(string userId);
}

public class AccountDeletionService : IAccountDeletionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AccountDeletionService> _logger;

    public AccountDeletionService(IUnitOfWork unitOfWork, ILogger<AccountDeletionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> GetUserDataCountAsync(string userId)
    {
        var accounts = await _unitOfWork.Accounts.GetByUserIdAsync(userId);
        var categories = await _unitOfWork.Categories.GetByUserIdAsync(userId);
        var transactions = await _unitOfWork.Transactions.GetByUserIdAsync(userId);
        var budgets = await _unitOfWork.Budgets.GetByUserIdAsync(userId);
        var recurring = await _unitOfWork.RecurringTransactions.GetByUserIdAsync(userId);

        return accounts.Count() + categories.Count() + transactions.Count() + budgets.Count() + recurring.Count();
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
            // Exclui todos os dados do usuário com queries filtradas por userId (sem carregar tudo em memória)
            await _unitOfWork.RecurringTransactions.DeleteManyByUserIdAsync(userId);
            await _unitOfWork.Budgets.DeleteManyByUserIdAsync(userId);
            await _unitOfWork.Transactions.DeleteManyByUserIdAsync(userId);
            await _unitOfWork.Accounts.DeleteManyByUserIdAsync(userId);
            await _unitOfWork.Categories.DeleteManyByUserIdAsync(userId);

            await _unitOfWork.Users.DeleteAsync(userId);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar conta do usuário {UserId}", userId);
            throw new Exception("Erro ao deletar conta. Por favor, tente novamente.", ex);
        }
    }
}

