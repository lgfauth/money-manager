using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface ITransactionService
{
    Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request);
    Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId);
    Task<PagedResultDto<TransactionResponseDto>> GetAllPagedAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string? accountId = null,
        string sortBy = "date_desc");
    Task<TransactionResponseDto> GetByIdAsync(string userId, string id);
    Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;

    public TransactionService(
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
    }

    public async Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request)
    {
        _processLogger.AddStep("Creating transaction", new Dictionary<string, object?>
        {
            ["type"] = request.Type,
            ["accountId"] = request.AccountId
        });

        if (!string.IsNullOrEmpty(request.ClientRequestId))
        {
            var existing = await _unitOfWork.Transactions.GetByClientRequestIdAsync(userId, request.ClientRequestId);
            if (existing != null)
            {
                _processLogger.AddStep("Duplicate request detected, returning existing transaction", new Dictionary<string, object?>
                {
                    ["clientRequestId"] = request.ClientRequestId,
                    ["existingTransactionId"] = existing.Id
                });
                return await MapToDtoAsync(userId, existing);
            }
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
        {
            _processLogger.AddWarning("Account not found", new Dictionary<string, object?> { ["accountId"] = request.AccountId });
            throw new KeyNotFoundException("Account not found");
        }

        var transactionType = request.Type;

        if (transactionType == TransactionType.Transfer)
        {
            _processLogger.AddStep("Processing transfer", new Dictionary<string, object?>
            {
                ["fromAccount"] = request.AccountId,
                ["toAccount"] = request.ToAccountId
            });

            if (string.IsNullOrEmpty(request.ToAccountId))
                throw new InvalidOperationException("ToAccountId is required for transfers");

            var toAccount = await _unitOfWork.Accounts.GetByIdAsync(request.ToAccountId);
            if (toAccount == null || toAccount.UserId != userId || toAccount.IsDeleted)
                throw new KeyNotFoundException("Destination account not found");

            account.Balance -= request.Amount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            try
            {
                toAccount.Balance += request.Amount;
                toAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(toAccount);
            }
            catch
            {
                _processLogger.AddWarning("Transfer failed on destination update, rolling back source");
                account.Balance += request.Amount;
                account.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(account);
                throw;
            }

            _processLogger.AddStep("Transfer processed");
        }
        else
        {
            var impactAmount = transactionType == TransactionType.Income ? request.Amount : -request.Amount;
            account.Balance += impactAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            _processLogger.AddStep("Balance updated", new Dictionary<string, object?> { ["impact"] = impactAmount });
        }

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            CategoryId = request.CategoryId,
            Type = transactionType,
            Amount = request.Amount,
            Currency = account.Currency,
            Date = request.Date,
            Description = request.Description,
            Tags = request.Tags,
            Notes = request.Notes,
            ToAccountId = request.ToAccountId,
            Status = request.Status,
            ClientRequestId = request.ClientRequestId
        };

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Transaction created", new Dictionary<string, object?> { ["transactionId"] = transaction.Id });

        return await MapToDtoAsync(userId, transaction, account);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        return await MapToDtosAsync(
            userId,
            transactions.Where(t => t.UserId == userId && !t.IsDeleted).ToList());
    }

    public async Task<PagedResultDto<TransactionResponseDto>> GetAllPagedAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string? accountId = null,
        string sortBy = "date_desc")
    {
        var (items, totalCount) = await _unitOfWork.Transactions.GetPagedByUserAsync(
            userId, page, pageSize, startDate, endDate, type, accountId, sortBy);

        var mappedItems = await MapToDtosAsync(userId, items.ToList());

        return new PagedResultDto<TransactionResponseDto>
        {
            Items = mappedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TransactionResponseDto> GetByIdAsync(string userId, string id)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        return await MapToDtoAsync(userId, transaction);
    }

    public async Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request)
    {
        _processLogger.AddStep("Updating transaction", new Dictionary<string, object?> { ["transactionId"] = id });

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        _processLogger.AddStep("Reverting previous transaction impact");
        await RevertTransactionImpact(userId, transaction);

        transaction.AccountId = request.AccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Tags = request.Tags;
        transaction.Notes = request.Notes;
        transaction.ToAccountId = request.ToAccountId;
        transaction.Status = request.Status;
        transaction.UpdatedAt = DateTime.UtcNow;

        var newAccount = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (newAccount == null || newAccount.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        _processLogger.AddStep("Applying new transaction impact");
        await ApplyTransactionImpact(userId, transaction);

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Transaction updated successfully");
        return await MapToDtoAsync(userId, transaction, newAccount);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        _processLogger.AddStep("Deleting transaction", new Dictionary<string, object?> { ["transactionId"] = id });

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        await RevertTransactionImpact(userId, transaction);

        transaction.IsDeleted = true;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Transaction deleted (soft delete)");
    }

    private async Task ApplyTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Transfer)
        {
            if (!string.IsNullOrEmpty(transaction.ToAccountId))
            {
                var sourceAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
                if (sourceAccount == null || sourceAccount.UserId != userId || sourceAccount.IsDeleted)
                    throw new KeyNotFoundException("Source account not found");

                var toAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.ToAccountId);
                if (toAccount == null || toAccount.UserId != userId || toAccount.IsDeleted)
                    throw new KeyNotFoundException("Destination account not found");

                sourceAccount.Balance -= transaction.Amount;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance += transaction.Amount;
                    toAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(toAccount);
                }
                catch
                {
                    sourceAccount.Balance += transaction.Amount;
                    sourceAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(sourceAccount);
                    throw;
                }
            }
        }
        else
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
            if (account == null || account.UserId != userId || account.IsDeleted)
                throw new KeyNotFoundException("Account not found");

            var impactAmount = transaction.Type == TransactionType.Income ? transaction.Amount : -transaction.Amount;
            account.Balance += impactAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }
    }

    private async Task RevertTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.Type == TransactionType.Transfer)
        {
            if (!string.IsNullOrEmpty(transaction.ToAccountId))
            {
                var sourceAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
                if (sourceAccount == null || sourceAccount.UserId != userId || sourceAccount.IsDeleted)
                    throw new KeyNotFoundException("Source account not found");

                var toAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.ToAccountId);
                if (toAccount == null || toAccount.UserId != userId || toAccount.IsDeleted)
                    throw new KeyNotFoundException("Destination account not found");

                sourceAccount.Balance += transaction.Amount;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance -= transaction.Amount;
                    toAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(toAccount);
                }
                catch
                {
                    sourceAccount.Balance -= transaction.Amount;
                    sourceAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(sourceAccount);
                    throw;
                }
            }
        }
        else
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);
            if (account == null || account.UserId != userId || account.IsDeleted)
                throw new KeyNotFoundException("Account not found");

            var revertAmount = transaction.Type == TransactionType.Income ? -transaction.Amount : transaction.Amount;
            account.Balance += revertAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }
    }

    private async Task<List<TransactionResponseDto>> MapToDtosAsync(string userId, IReadOnlyCollection<Transaction> transactions)
    {
        if (transactions.Count == 0)
        {
            return [];
        }

        var accountIds = transactions
            .Select(t => t.AccountId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToHashSet();

        var categoryIds = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.CategoryId))
            .Select(t => t.CategoryId!)
            .Distinct()
            .ToHashSet();

        var accounts = (await _unitOfWork.Accounts.GetAllAsync())
            .Where(account => account.UserId == userId && !account.IsDeleted && accountIds.Contains(account.Id))
            .ToDictionary(account => account.Id);

        var categories = (await _unitOfWork.Categories.GetAllAsync())
            .Where(category => category.UserId == userId && !category.IsDeleted && categoryIds.Contains(category.Id))
            .ToDictionary(category => category.Id);

        return transactions
            .Select(transaction =>
            {
                accounts.TryGetValue(transaction.AccountId, out var account);
                var category = transaction.CategoryId != null && categories.TryGetValue(transaction.CategoryId, out var resolvedCategory)
                    ? resolvedCategory
                    : null;

                return MapToDto(transaction, account, category);
            })
            .ToList();
    }

    private async Task<TransactionResponseDto> MapToDtoAsync(
        string userId,
        Transaction transaction,
        Account? account = null,
        Category? category = null)
    {
        account ??= await _unitOfWork.Accounts.GetByIdAsync(transaction.AccountId);

        if (!string.IsNullOrWhiteSpace(transaction.CategoryId) && category == null)
        {
            var resolvedCategory = await _unitOfWork.Categories.GetByIdAsync(transaction.CategoryId);
            if (resolvedCategory != null && resolvedCategory.UserId == userId && !resolvedCategory.IsDeleted)
            {
                category = resolvedCategory;
            }
        }

        if (account != null && (account.UserId != userId || account.IsDeleted))
        {
            account = null;
        }

        return MapToDto(transaction, account, category);
    }

    private static TransactionResponseDto MapToDto(Transaction transaction, Account? account = null, Category? category = null)
    {
        return new TransactionResponseDto
        {
            Id = transaction.Id,
            AccountId = transaction.AccountId,
            AccountName = account?.Name ?? string.Empty,
            AccountColor = account?.Color ?? "#00C896",
            CategoryId = transaction.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            CategoryColor = category?.Color ?? "#64748b",
            Type = transaction.Type,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            Date = transaction.Date,
            Description = transaction.Description,
            Tags = transaction.Tags,
            Notes = transaction.Notes,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
