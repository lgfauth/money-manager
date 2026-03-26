using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using Microsoft.Extensions.Logging;

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
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        ILogger<TransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request)
    {
        _logger.LogDebug("Creating transaction: userId={UserId}, type={Type}, amount={Amount}, accountId={AccountId}",
            userId, request.Type, request.Amount, request.AccountId);

        // Idempotência: se ClientRequestId fornecido, verificar duplicata
        if (!string.IsNullOrEmpty(request.ClientRequestId))
        {
            var existing = await _unitOfWork.Transactions.GetByClientRequestIdAsync(userId, request.ClientRequestId);
            if (existing != null)
            {
                _logger.LogInformation("Duplicate request detected (ClientRequestId={ClientRequestId}), returning existing transaction {TransactionId}",
                    request.ClientRequestId, existing.Id);
                return MapToDto(existing);
            }
        }

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
        {
            _logger.LogWarning("Account {AccountId} not found for user {UserId}", request.AccountId, userId);
            throw new KeyNotFoundException("Account not found");
        }

        var transactionType = (TransactionType)request.Type;

        if (transactionType == TransactionType.Transfer)
        {
            _logger.LogDebug("Processing transfer from {FromAccount} to {ToAccount}, amount: {Amount}",
                request.AccountId, request.ToAccountId, request.Amount);

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
                _logger.LogWarning("Transfer failed on destination update. Rolling back source account {AccountId}", account.Id);
                account.Balance -= sourceImpact;
                account.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(account);
                throw;
            }

            _logger.LogInformation("Transfer processed: {Amount} from {FromAccount} to {ToAccount}",
                request.Amount, request.AccountId, request.ToAccountId);
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
                    _logger.LogWarning("Credit limit exceeded for account {AccountId}: limit={Limit}, current={Current}, attempt={Attempt}",
                        account.Id, account.CreditLimit.Value, currentDebt, request.Amount);
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido. Disponível: R$ {available:F2} | Tentando usar: R$ {request.Amount:F2}");
                }
            }

            var impactAmount = CalculateBalanceImpact(account.Type, transactionType, request.Amount);
            account.Balance += impactAmount;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            _logger.LogInformation("Balance updated for account {AccountId}: impact={Impact}, type={Type}",
                request.AccountId, impactAmount, transactionType);
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
            _logger.LogDebug("Determining invoice for credit card transaction on account {AccountId}, date {Date}",
                account.Id, request.Date);

            var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
            transaction.InvoiceId = invoice.Id;

            invoice.TotalAmount += request.Amount;
            invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);

            _logger.LogInformation("Transaction linked to invoice {InvoiceId} (Reference: {RefMonth})",
                invoice.Id, invoice.ReferenceMonth);
        }

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} created successfully for user {UserId}",
            transaction.Id, userId);

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
        _logger.LogDebug("Updating transaction {TransactionId} for user {UserId}", id, userId);

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        var oldInvoiceId = transaction.InvoiceId;

        // Revert previous impact
        _logger.LogDebug("Reverting previous transaction impact for {TransactionId}", id);
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
        _logger.LogDebug("Applying new transaction impact for {TransactionId}", id);
        await ApplyTransactionImpact(userId, transaction);

        // Gerenciar faturas se for cartão de crédito
        if (newAccount.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense)
        {
            var newInvoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
            transaction.InvoiceId = newInvoice.Id;

            if (!string.IsNullOrEmpty(oldInvoiceId) && oldInvoiceId != newInvoice.Id)
            {
                _logger.LogInformation("Transaction moved from invoice {OldInvoice} to {NewInvoice}",
                    oldInvoiceId, newInvoice.Id);
                await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
            }

            await _invoiceService.RecalculateInvoiceTotalAsync(userId, newInvoice.Id);
            _logger.LogDebug("Invoice totals recalculated after transaction update");
        }
        else if (!string.IsNullOrEmpty(oldInvoiceId))
        {
            transaction.InvoiceId = null;
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
            _logger.LogDebug("Transaction removed from invoice {InvoiceId}", oldInvoiceId);
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} updated successfully", id);
        return MapToDto(transaction);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        _logger.LogDebug("Deleting transaction {TransactionId} for user {UserId}", id, userId);

        var transaction = await _unitOfWork.Transactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        var invoiceId = transaction.InvoiceId;

        // Revert impact
        _logger.LogDebug("Reverting transaction impact before deletion for {TransactionId}", id);
        await RevertTransactionImpact(userId, transaction);

        transaction.IsDeleted = true;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Transactions.UpdateAsync(transaction);

        if (!string.IsNullOrEmpty(invoiceId))
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
            _logger.LogInformation("Invoice {InvoiceId} recalculated after transaction deletion", invoiceId);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Transaction {TransactionId} deleted (soft delete) successfully", id);
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
                    _logger.LogWarning("Transfer apply failed on destination. Rolling back source account {AccountId}", sourceAccount.Id);
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
                    _logger.LogWarning("Transfer revert failed on destination. Rolling back source account {AccountId}", sourceAccount.Id);
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
