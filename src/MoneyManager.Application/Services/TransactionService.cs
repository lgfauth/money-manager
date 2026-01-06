using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface ITransactionService
{
    Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request);
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId);
    Task<TransactionResponseDto> GetByIdAsync(string userId, string id);
    Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountService _accountService;

    public TransactionService(IUnitOfWork unitOfWork, IAccountService accountService)
    {
        _unitOfWork = unitOfWork;
        _accountService = accountService;
    }

    public async Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        var transactionType = (TransactionType)request.Type;

        if (transactionType == TransactionType.Transfer)
        {
            if (string.IsNullOrEmpty(request.ToAccountId))
                throw new InvalidOperationException("ToAccountId is required for transfers");

            var toAccount = await _unitOfWork.Accounts.GetByIdAsync(request.ToAccountId);
            if (toAccount == null || toAccount.UserId != userId)
                throw new KeyNotFoundException("Destination account not found");

            // Debit from source
            await _accountService.UpdateBalanceAsync(userId, request.AccountId, -request.Amount);
            // Credit to destination
            await _accountService.UpdateBalanceAsync(userId, request.ToAccountId, request.Amount);
        }
        else
        {
            var impactAmount = transactionType == TransactionType.Income ? request.Amount : -request.Amount;

            // Credit card balance represents debt (invoice amount).
            // Expense increases debt; Income decreases debt.
            if (account.Type == AccountType.CreditCard)
            {
                impactAmount = transactionType == TransactionType.Income ? -request.Amount : request.Amount;
            }
            await _accountService.UpdateBalanceAsync(userId, request.AccountId, impactAmount);
        }

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Type = transactionType,
            Amount = request.Amount,
            Date = request.Date,
            Description = request.Description,
            Tags = request.Tags,
            ToAccountId = request.ToAccountId,
            Status = (TransactionStatus)request.Status
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(transaction);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        return transactions
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .Select(MapToDto);
    }

    public async Task<TransactionResponseDto> GetByIdAsync(string userId, string id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        return MapToDto(transaction);
    }

    public async Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        // Revert previous impact
        await RevertTransactionImpact(userId, transaction);

        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Type = (TransactionType)request.Type;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Tags = request.Tags;
        transaction.ToAccountId = request.ToAccountId;
        transaction.Status = (TransactionStatus)request.Status;
        transaction.UpdatedAt = DateTime.UtcNow;

        // Apply new impact
        await ApplyTransactionImpact(userId, transaction);

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(transaction);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        // Revert impact
        await RevertTransactionImpact(userId, transaction);

        transaction.IsDeleted = true;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task ApplyTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Transfer)
        {
            if (!string.IsNullOrEmpty(transaction.ToAccountId))
            {
                await _accountService.UpdateBalanceAsync(userId, transaction.AccountId, -transaction.Amount);
                await _accountService.UpdateBalanceAsync(userId, transaction.ToAccountId, transaction.Amount);
            }
        }
        else
        {
            var impactAmount = transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;

            var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
            if (account == null || account.UserId != userId || account.IsDeleted)
                throw new KeyNotFoundException("Account not found");

            if (account.Type == AccountType.CreditCard)
            {
                impactAmount = transaction.Type == TransactionType.Income ? -transaction.Amount : transaction.Amount;
            }
            await _accountService.UpdateBalanceAsync(userId, transaction.AccountId, impactAmount);
        }
    }

    private async Task RevertTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Transfer)
        {
            if (!string.IsNullOrEmpty(transaction.ToAccountId))
            {
                await _accountService.UpdateBalanceAsync(userId, transaction.AccountId, transaction.Amount);
                await _accountService.UpdateBalanceAsync(userId, transaction.ToAccountId, -transaction.Amount);
            }
        }
        else
        {
            var impactAmount = transaction.Type == TransactionType.Income ? -transaction.Amount : transaction.Amount;

            var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
            if (account == null || account.UserId != userId || account.IsDeleted)
                throw new KeyNotFoundException("Account not found");

            if (account.Type == AccountType.CreditCard)
            {
                impactAmount = transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
            }
            await _accountService.UpdateBalanceAsync(userId, transaction.AccountId, impactAmount);
        }
    }

    private static TransactionResponseDto MapToDto(Transaction transaction)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            CategoryId = transaction.CategoryId,
            Type = (int)transaction.Type,
            Amount = transaction.Amount,
            Date = transaction.Date,
            Description = transaction.Description,
            Tags = transaction.Tags,
            Status = (int)transaction.Status,
            CreatedAt = transaction.CreatedAt
        };
    }
}
