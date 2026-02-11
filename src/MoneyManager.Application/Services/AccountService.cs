using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
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
        var type = (AccountType)request.Type;
        int? invoiceClosingDay = type == AccountType.CreditCard ? (request.InvoiceClosingDay ?? 1) : null;
        int invoiceDueDayOffset = type == AccountType.CreditCard ? request.InvoiceDueDayOffset : 7;
        decimal? creditLimit = type == AccountType.CreditCard ? request.CreditLimit : null;

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Type = type,
            Balance = request.InitialBalance,
            InitialBalance = request.InitialBalance,
            InvoiceClosingDay = invoiceClosingDay,
            InvoiceDueDayOffset = invoiceDueDayOffset,
            CreditLimit = creditLimit
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
        account.Type = (AccountType)request.Type;
        account.InvoiceClosingDay = account.Type == AccountType.CreditCard ? (request.InvoiceClosingDay ?? 1) : null;
        account.InvoiceDueDayOffset = account.Type == AccountType.CreditCard ? request.InvoiceDueDayOffset : 7;
        account.CreditLimit = account.Type == AccountType.CreditCard ? request.CreditLimit : null;
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
            Type = (int)account.Type,
            Balance = account.Balance,
            InitialBalance = account.InitialBalance,
            CreditLimit = account.CreditLimit,
            InvoiceClosingDay = account.InvoiceClosingDay,
            InvoiceDueDayOffset = account.InvoiceDueDayOffset,
            LastInvoiceClosedAt = account.LastInvoiceClosedAt,
            CurrentOpenInvoiceId = account.CurrentOpenInvoiceId,
            CreatedAt = account.CreatedAt
        };
    }
}
