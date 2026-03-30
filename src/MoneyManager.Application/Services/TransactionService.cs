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
        string sortBy = "date_desc");
    Task<TransactionResponseDto> GetByIdAsync(string userId, string id);
    Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class TransactionService : ITransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly IProcessLogger _processLogger;

    public TransactionService(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _processLogger = processLogger;
    }

    public async Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request)
    {
        _processLogger.AddStep("Creating transaction", new Dictionary<string, object?>
        {
            ["type"] = request.Type,
            ["accountId"] = request.AccountId
        });

        // Idempotência: se ClientRequestId fornecido, verificar duplicata
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
                return MapToDto(existing);
            }
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
        {
            _processLogger.AddWarning("Account not found", new Dictionary<string, object?>
            {
                ["accountId"] = request.AccountId
            });
            throw new KeyNotFoundException("Account not found");
        }

        var transactionType = (TransactionType)request.Type;

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

            var sourceImpact = -request.Amount;
            var destImpact = toAccount.Type == AccountType.CreditCard ? -request.Amount : request.Amount;

            account.Balance += sourceImpact;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            try
            {
                toAccount.Balance += destImpact;
                toAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(toAccount);
            }
            catch
            {
                _processLogger.AddWarning("Transfer failed on destination update, rolling back source");
                account.Balance -= sourceImpact;
                account.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(account);
                throw;
            }

            _processLogger.AddStep("Transfer processed");
        }
        else
        {
            // Para despesas em cartão de crédito, validar limite antes de criar
            if (account.Type == AccountType.CreditCard &&
                transactionType == TransactionType.Expense &&
                account.CreditLimit.HasValue)
            {
                var currentDebt = Math.Abs(account.Balance);
                var newDebt = currentDebt + request.Amount;

                if (newDebt > account.CreditLimit.Value)
                {
                    var available = account.CreditLimit.Value - currentDebt;
                    _processLogger.AddWarning("Credit limit exceeded", new Dictionary<string, object?>
                    {
                        ["limit"] = account.CreditLimit.Value,
                        ["currentDebt"] = currentDebt,
                        ["attempted"] = request.Amount
                    });
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido. Disponível: R$ {available:F2} | Tentando usar: R$ {request.Amount:F2}");
                }
            }

            var impactAmount = CalculateBalanceImpact(account.Type, transactionType, request.Amount);
            account.Balance += impactAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            _processLogger.AddStep("Balance updated", new Dictionary<string, object?>
            {
                ["impact"] = impactAmount,
                ["accountType"] = account.Type.ToString()
            });
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
            Status = (TransactionStatus)request.Status,
            ClientRequestId = request.ClientRequestId
        };

        // Se for despesa em cartão de crédito, vincular à fatura apropriada
        if (account.Type == AccountType.CreditCard && transactionType == TransactionType.Expense)
        {
            var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
            transaction.InvoiceId = invoice.Id;

            invoice.TotalAmount += request.Amount;
            invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);

            _processLogger.AddStep("Transaction linked to invoice", new Dictionary<string, object?>
            {
                ["invoiceId"] = invoice.Id,
                ["referenceMonth"] = invoice.ReferenceMonth
            });
        }

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Transaction created", new Dictionary<string, object?>
        {
            ["transactionId"] = transaction.Id
        });

        return MapToDto(transaction);
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        return transactions
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .Select(MapToDto);
    }

    public async Task<PagedResultDto<TransactionResponseDto>> GetAllPagedAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransactionType? type = null,
        string sortBy = "date_desc")
    {
        var (items, totalCount) = await _unitOfWork.Transactions.GetPagedByUserAsync(
            userId, page, pageSize, startDate, endDate, type, sortBy);

        return new PagedResultDto<TransactionResponseDto>
        {
            Items = items.Select(MapToDto),
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

        return MapToDto(transaction);
    }

    public async Task<TransactionResponseDto> UpdateAsync(string userId, string id, CreateTransactionRequestDto request)
    {
        _processLogger.AddStep("Updating transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = id
        });

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        var oldInvoiceId = transaction.InvoiceId;

        // Revert previous impact
        _processLogger.AddStep("Reverting previous transaction impact");
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

        // Buscar nova conta
        var newAccount = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (newAccount == null || newAccount.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        // Apply new impact
        _processLogger.AddStep("Applying new transaction impact");
        await ApplyTransactionImpact(userId, transaction);

        // Gerenciar faturas se for cartão de crédito
        if (newAccount.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense)
        {
            var newInvoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
            transaction.InvoiceId = newInvoice.Id;

            if (!string.IsNullOrEmpty(oldInvoiceId) && oldInvoiceId != newInvoice.Id)
            {
                _processLogger.AddStep("Transaction moved between invoices", new Dictionary<string, object?>
                {
                    ["oldInvoice"] = oldInvoiceId,
                    ["newInvoice"] = newInvoice.Id
                });
                await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
            }

            await _invoiceService.RecalculateInvoiceTotalAsync(userId, newInvoice.Id);
        }
        else if (!string.IsNullOrEmpty(oldInvoiceId))
        {
            transaction.InvoiceId = null;
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Transaction updated successfully");
        return MapToDto(transaction);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        _processLogger.AddStep("Deleting transaction", new Dictionary<string, object?>
        {
            ["transactionId"] = id
        });

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        var invoiceId = transaction.InvoiceId;

        // Revert impact
        await RevertTransactionImpact(userId, transaction);

        transaction.IsDeleted = true;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Transactions.UpdateAsync(transaction);

        if (!string.IsNullOrEmpty(invoiceId))
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
            _processLogger.AddStep("Invoice recalculated after deletion", new Dictionary<string, object?>
            {
                ["invoiceId"] = invoiceId
            });
        }

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

                var sourceImpact = -transaction.Amount;
                var destImpact = toAccount.Type == AccountType.CreditCard ? -transaction.Amount : transaction.Amount;

                sourceAccount.Balance += sourceImpact;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance += destImpact;
                    toAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(toAccount);
                }
                catch
                {
                    sourceAccount.Balance -= sourceImpact;
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

            var impactAmount = CalculateBalanceImpact(account.Type, transaction.Type, transaction.Amount);
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

                var sourceRevert = transaction.Amount;
                var destRevert = toAccount.Type == AccountType.CreditCard ? transaction.Amount : -transaction.Amount;

                sourceAccount.Balance += sourceRevert;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance += destRevert;
                    toAccount.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Accounts.UpdateAsync(toAccount);
                }
                catch
                {
                    sourceAccount.Balance -= sourceRevert;
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

            var impactAmount = -CalculateBalanceImpact(account.Type, transaction.Type, transaction.Amount);
            account.Balance += impactAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }
    }

    private static decimal CalculateBalanceImpact(AccountType accountType, TransactionType transactionType, decimal amount)
    {
        var impact = transactionType == TransactionType.Income ? amount : -amount;
        if (accountType == AccountType.CreditCard)
        {
            impact = -impact;
        }
        return impact;
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
