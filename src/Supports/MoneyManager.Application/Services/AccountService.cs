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
        var type = request.Type;
        int? invoiceClosingDay = type == AccountType.CreditCard ? (request.InvoiceClosingDay ?? 1) : null;
        int invoiceDueDayOffset = type == AccountType.CreditCard ? request.InvoiceDueDayOffset : 7;
        decimal? creditLimit = type == AccountType.CreditCard ? request.CreditLimit : null;
        decimal committedCredit = type == AccountType.CreditCard ? Math.Abs(request.InitialBalance) : 0m;

        var account = new Account
        {
            UserId = userId,
            Name = request.Name,
            Type = type,
            Balance = request.InitialBalance,
            InitialBalance = request.InitialBalance,
            Currency = request.Currency,
            Color = request.Color,
            InvoiceClosingDay = invoiceClosingDay,
            InvoiceDueDayOffset = invoiceDueDayOffset,
            CreditLimit = creditLimit,
            CommittedCredit = committedCredit
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

        var existingType = account.Type;

        account.Name = request.Name;
        account.Type = request.Type;
        account.Currency = request.Currency;
        account.Color = request.Color;
        account.InvoiceClosingDay = account.Type == AccountType.CreditCard ? (request.InvoiceClosingDay ?? 1) : null;
        account.InvoiceDueDayOffset = account.Type == AccountType.CreditCard ? request.InvoiceDueDayOffset : 7;
        account.CreditLimit = account.Type == AccountType.CreditCard ? request.CreditLimit : null;
        account.CommittedCredit = account.Type switch
        {
            AccountType.CreditCard when existingType != AccountType.CreditCard => Math.Abs(account.Balance),
            AccountType.CreditCard => account.CommittedCredit,
            _ => 0m
        };
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

        // Cascade soft delete: transações vinculadas a esta conta
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

        // Cascade soft delete: faturas de cartão de crédito vinculadas a esta conta
        var accountInvoices = await _unitOfWork.CreditCardInvoices.GetByAccountIdAsync(id);
        foreach (var invoice in accountInvoices.Where(i => !i.IsDeleted))
        {
            invoice.IsDeleted = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
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
        var committedCredit = GetCommittedCreditSnapshot(account);
        var availableCredit = account.Type == AccountType.CreditCard && account.CreditLimit.HasValue
            ? Math.Max(0m, account.CreditLimit.Value - committedCredit)
            : 0m;

        return new AccountResponseDto
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Balance = account.Balance,
            InitialBalance = account.InitialBalance,
            Currency = account.Currency,
            Color = account.Color,
            CreditLimit = account.CreditLimit,
            CommittedCredit = committedCredit,
            AvailableCredit = availableCredit,
            InvoiceClosingDay = account.InvoiceClosingDay,
            InvoiceDueDayOffset = account.InvoiceDueDayOffset,
            LastInvoiceClosedAt = account.LastInvoiceClosedAt,
            CurrentOpenInvoiceId = account.CurrentOpenInvoiceId,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    private static decimal GetCommittedCreditSnapshot(Account account)
    {
        if (account.Type != AccountType.CreditCard)
        {
            return 0m;
        }

        return Math.Max(account.CommittedCredit, Math.Abs(account.Balance));
    }
}
