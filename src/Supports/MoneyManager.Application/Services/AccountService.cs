using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

public interface IAccountService
{
    Task<AccountResponseDto> CreateAsync(string userId, CreateAccountRequestDto request);
    Task<IEnumerable<AccountResponseDto>> GetAllAsync(string userId);
    Task<AccountResponseDto> GetByIdAsync(string userId, string id);
    Task<AccountResponseDto> UpdateAsync(string userId, string id, CreateAccountRequestDto request);
    Task DeleteAsync(string userId, string id);
    Task UpdateBalanceAsync(string userId, string accountId, decimal amount);
}

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponseDto> CreateAsync(string userId, CreateAccountRequestDto request)
    {
        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Type = request.Type,
            Balance = request.InitialBalance,
            InitialBalance = request.InitialBalance,
            Currency = request.Currency,
            Color = request.Color
        };

        await _unitOfWork.Accounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task<IEnumerable<AccountResponseDto>> GetAllAsync(string userId)
    {
        var accounts = await _unitOfWork.Accounts.GetAllAsync();
        return accounts
            .Where(a => a.UserId == userId && !a.IsDeleted)
            .Select(MapToDto);
    }

    public async Task<AccountResponseDto> GetByIdAsync(string userId, string id)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        return MapToDto(account);
    }

    public async Task<AccountResponseDto> UpdateAsync(string userId, string id, CreateAccountRequestDto request)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        account.Name = request.Name;
        account.Type = request.Type;
        account.Currency = request.Currency;
        account.Color = request.Color;
        account.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Accounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        account.IsDeleted = true;
        account.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Accounts.UpdateAsync(account);

        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        var accountTransactions = allTransactions
            .Where(t => t.UserId == userId && t.AccountId == id && !t.IsDeleted)
            .ToList();

        foreach (var transaction in accountTransactions)
        {
            transaction.IsDeleted = true;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateBalanceAsync(string userId, string accountId, decimal amount)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Accounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();
    }

    private static AccountResponseDto MapToDto(Account account)
    {
        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            InitialBalance = account.InitialBalance,
            Currency = account.Currency,
            Color = account.Color,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
