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
    private readonly IAccountService _accountService;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IUnitOfWork unitOfWork,
        IAccountService accountService,
        ICreditCardInvoiceService invoiceService,
        ILogger<TransactionService> logger)
    {
        _unitOfWork = unitOfWork;
        _accountService = accountService;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    public async Task<TransactionResponseDto> CreateAsync(string userId, CreateTransactionRequestDto request)
    {
        _logger.LogDebug("Creating transaction: userId={UserId}, type={Type}, amount={Amount}, accountId={AccountId}",
            userId, request.Type, request.Amount, request.AccountId);

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId)
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
            if (toAccount == null || toAccount.UserId != userId)
                throw new KeyNotFoundException("Destination account not found");

            // Debit from source
            await _accountService.UpdateBalanceAsync(userId, request.AccountId, -request.Amount);
            // Credit to destination
            var toImpact = request.Amount;
            if (toAccount.Type == AccountType.CreditCard)
            {
                // Credit card balance is debt; transferring money to the card reduces debt.
                toImpact = -request.Amount;
            }
            await _accountService.UpdateBalanceAsync(userId, request.ToAccountId, toImpact);

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

            var impactAmount = transactionType == TransactionType.Income ? request.Amount : -request.Amount;

            // Credit card balance represents debt (invoice amount).
            // Expense increases debt; Income decreases debt.
            if (account.Type == AccountType.CreditCard)
            {
                impactAmount = transactionType == TransactionType.Income ? -request.Amount : request.Amount;
            }
            await _accountService.UpdateBalanceAsync(userId, request.AccountId, impactAmount);

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
            Status = (TransactionStatus)request.Status
        };

        // Se for despesa em cartão de crédito, vincular à fatura apropriada
        if (account.Type == AccountType.CreditCard && transactionType == TransactionType.Expense)
        {
            _logger.LogDebug("Determining invoice for credit card transaction on account {AccountId}, date {Date}",
                account.Id, request.Date);

            try
            {
                var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
                transaction.InvoiceId = invoice.Id;

                // Atualizar total da fatura
                invoice.TotalAmount += request.Amount;
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);

                _logger.LogInformation("Transaction linked to invoice {InvoiceId} (Reference: {RefMonth})",
                    invoice.Id, invoice.ReferenceMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to link transaction to invoice for account {AccountId}", account.Id);
                // Continuar mesmo se falhar (não bloqueante)
            }
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
        var oldAccountId = transaction.AccountId;
        var oldDate = transaction.Date;
        var oldAmount = transaction.Amount;

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
            try
            {
                // Determinar nova fatura baseado na nova data
                var newInvoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
                transaction.InvoiceId = newInvoice.Id;

                // Se mudou de fatura, recalcular ambas
                if (!string.IsNullOrEmpty(oldInvoiceId) && oldInvoiceId != newInvoice.Id)
                {
                    _logger.LogInformation("Transaction moved from invoice {OldInvoice} to {NewInvoice}",
                        oldInvoiceId, newInvoice.Id);

                    // Recalcular fatura antiga
                    await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
                }

                // Recalcular fatura nova/atual
                await _invoiceService.RecalculateInvoiceTotalAsync(userId, newInvoice.Id);

                _logger.LogDebug("Invoice totals recalculated after transaction update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update invoice for transaction {TransactionId}", id);
            }
        }
        else if (!string.IsNullOrEmpty(oldInvoiceId))
        {
            // Se a transação estava em uma fatura mas agora não é mais cartão de crédito (mudou conta)
            try
            {
                transaction.InvoiceId = null;
                await _invoiceService.RecalculateInvoiceTotalAsync(userId, oldInvoiceId);
                _logger.LogDebug("Transaction removed from invoice {InvoiceId}", oldInvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove transaction from invoice {InvoiceId}", oldInvoiceId);
            }
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

        // Se a transação estava vinculada a uma fatura, recalcular o total
        if (!string.IsNullOrEmpty(invoiceId))
        {
            try
            {
                await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
                _logger.LogInformation("Invoice {InvoiceId} recalculated after transaction deletion", invoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recalculate invoice {InvoiceId} after deleting transaction", invoiceId);
            }
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
                await _accountService.UpdateBalanceAsync(userId, transaction.AccountId, -transaction.Amount);

                var toAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.ToAccountId);
                if (toAccount == null || toAccount.UserId != userId || toAccount.IsDeleted)
                    throw new KeyNotFoundException("Destination account not found");

                var toImpact = transaction.Amount;
                if (toAccount.Type == AccountType.CreditCard)
                {
                    toImpact = -transaction.Amount;
                }

                await _accountService.UpdateBalanceAsync(userId, transaction.ToAccountId, toImpact);
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

                var toAccount = await _unitOfWork.Accounts.GetByIdAsync(transaction.ToAccountId);
                if (toAccount == null || toAccount.UserId != userId || toAccount.IsDeleted)
                    throw new KeyNotFoundException("Destination account not found");

                var toImpact = -transaction.Amount;
                if (toAccount.Type == AccountType.CreditCard)
                {
                    // Original transfer reduced debt; reverting it should increase debt back.
                    toImpact = transaction.Amount;
                }

                await _accountService.UpdateBalanceAsync(userId, transaction.ToAccountId, toImpact);
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
