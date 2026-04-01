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
    Task CreateInstallmentPurchaseAsync(string userId, InstallmentPurchaseRequestDto request);
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
                return await MapToDtoAsync(userId, existing);
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

            var sourceImpact = -request.Amount;
            var destImpact = toAccount.Type == AccountType.CreditCard
                ? CalculateCreditCardPaymentBalanceDelta(toAccount.Balance, request.Amount)
                : request.Amount;

            account.Balance += sourceImpact;
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);

            try
            {
                toAccount.Balance += destImpact;
                if (toAccount.Type == AccountType.CreditCard)
                {
                    toAccount.CommittedCredit = Math.Max(0m, GetCommittedCreditSnapshot(toAccount) - request.Amount);
                }
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
            if (!request.SkipCreditLimitValidation &&
                account.Type == AccountType.CreditCard &&
                transactionType == TransactionType.Expense &&
                account.CreditLimit.HasValue)
            {
                var currentCommittedCredit = GetCommittedCreditSnapshot(account);
                var newDebt = currentCommittedCredit + request.Amount;

                if (newDebt > account.CreditLimit.Value)
                {
                    var available = account.CreditLimit.Value - currentCommittedCredit;
                    _processLogger.AddWarning("Credit limit exceeded", new Dictionary<string, object?>
                    {
                        ["limit"] = account.CreditLimit.Value,
                        ["currentDebt"] = currentCommittedCredit,
                        ["attempted"] = request.Amount
                    });
                    throw new InvalidOperationException(
                        $"Limite de crédito excedido. Disponível: R$ {available:F2} | Tentando usar: R$ {request.Amount:F2}");
                }
            }

            if (!request.SkipAccountBalanceImpact)
            {
                var committedCreditBeforeImpact = account.Type == AccountType.CreditCard && transactionType == TransactionType.Expense
                    ? GetCommittedCreditSnapshot(account)
                    : 0m;

                var impactAmount = CalculateBalanceImpact(account.Type, transactionType, request.Amount);
                account.Balance += impactAmount;
                if (!request.SkipCommittedCreditImpact &&
                    account.Type == AccountType.CreditCard &&
                    transactionType == TransactionType.Expense)
                {
                    account.CommittedCredit = committedCreditBeforeImpact + request.Amount;
                }

                account.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(account);

                _processLogger.AddStep("Balance updated", new Dictionary<string, object?>
                {
                    ["impact"] = impactAmount,
                    ["accountType"] = account.Type.ToString(),
                    ["skipCommittedCreditImpact"] = request.SkipCommittedCreditImpact
                });
            }
            else
            {
                _processLogger.AddStep("Transaction created without account balance impact", new Dictionary<string, object?>
                {
                    ["accountId"] = account.Id,
                    ["type"] = transactionType.ToString()
                });
            }
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
            ClientRequestId = request.ClientRequestId,
            SkipAccountBalanceImpact = request.SkipAccountBalanceImpact,
            SkipCommittedCreditImpact = request.SkipCommittedCreditImpact,
            SkipCreditLimitValidation = request.SkipCreditLimitValidation,
            InstallmentGroupId = request.InstallmentGroupId,
            InstallmentNumber = request.InstallmentNumber,
            InstallmentCount = request.InstallmentCount
        };

        // Se for despesa em cartão de crédito, vincular à fatura apropriada
        if (account.Type == AccountType.CreditCard && transactionType == TransactionType.Expense)
        {
            var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, request.AccountId, request.Date);
            transaction.InvoiceId = invoice.Id;

            _processLogger.AddStep("Transaction linked to invoice", new Dictionary<string, object?>
            {
                ["invoiceId"] = invoice.Id,
                ["referenceMonth"] = invoice.ReferenceMonth
            });
        }

        await _unitOfWork.Transactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(transaction.InvoiceId))
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, transaction.InvoiceId);
        }

        _processLogger.AddStep("Transaction created", new Dictionary<string, object?>
        {
            ["transactionId"] = transaction.Id
        });

        return await MapToDtoAsync(userId, transaction, account);
    }

    public async Task CreateInstallmentPurchaseAsync(string userId, InstallmentPurchaseRequestDto request)
    {
        _processLogger.AddStep("Creating installment purchase", new Dictionary<string, object?>
        {
            ["accountId"] = request.AccountId,
            ["installmentCount"] = request.InstallmentCount,
            ["totalAmount"] = request.TotalAmount
        });

        if (request.InstallmentCount <= 1)
            throw new InvalidOperationException("Installment count must be greater than 1");

        if (request.TotalAmount <= 0)
            throw new InvalidOperationException("Installment total amount must be greater than zero");

        if (request.Type != TransactionType.Expense)
            throw new InvalidOperationException("Only expense transactions can be created as installments");

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        if (account.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Installment purchases are only supported for credit card accounts");

        var installmentGroupId = !string.IsNullOrWhiteSpace(request.ClientRequestId)
            ? request.ClientRequestId
            : Guid.NewGuid().ToString("N");

        if (!string.IsNullOrWhiteSpace(request.ClientRequestId))
        {
            var existingTransaction = await _unitOfWork.Transactions.GetByClientRequestIdAsync(userId, request.ClientRequestId);
            if (existingTransaction != null)
            {
                _processLogger.AddStep("Duplicate installment request detected via transaction", new Dictionary<string, object?>
                {
                    ["clientRequestId"] = request.ClientRequestId,
                    ["transactionId"] = existingTransaction.Id
                });
                return;
            }

            var existingSchedules = await _unitOfWork.RecurringTransactions.GetAllAsync();
            if (existingSchedules.Any(r => r.UserId == userId && !r.IsDeleted && r.InstallmentGroupId == request.ClientRequestId))
            {
                _processLogger.AddStep("Duplicate installment request detected via schedule", new Dictionary<string, object?>
                {
                    ["clientRequestId"] = request.ClientRequestId
                });
                return;
            }
        }

        ValidateCreditLimit(account, request.TotalAmount);

        var currentCommittedCredit = GetCommittedCreditSnapshot(account);
        account.Balance += CalculateBalanceImpact(account.Type, TransactionType.Expense, request.TotalAmount);
        account.CommittedCredit = currentCommittedCredit + request.TotalAmount;
        account.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Accounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        var installmentAmounts = CalculateInstallmentAmounts(request.TotalAmount, request.InstallmentCount);
        var firstScheduledMonthOffset = 1;
        var firstInstallmentNumberToSchedule = request.FirstInstallmentInCurrentInvoice ? 2 : 1;

        if (request.FirstInstallmentInCurrentInvoice)
        {
            await CreateAsync(userId, new CreateTransactionRequestDto
            {
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                Type = TransactionType.Expense,
                Amount = installmentAmounts[0],
                Date = request.Date,
                Description = BuildInstallmentDescription(request.Description, 1, request.InstallmentCount),
                Tags = request.Tags,
                Notes = request.Notes,
                Status = TransactionStatus.Pending,
                ClientRequestId = request.ClientRequestId,
                SkipAccountBalanceImpact = true,
                SkipCommittedCreditImpact = true,
                SkipCreditLimitValidation = true,
                InstallmentGroupId = installmentGroupId,
                InstallmentNumber = 1,
                InstallmentCount = request.InstallmentCount
            });
        }

        for (var installmentNumber = firstInstallmentNumberToSchedule; installmentNumber <= request.InstallmentCount; installmentNumber++)
        {
            var scheduledDate = CalculateInstallmentPostingDate(
                request.Date.Date,
                account.InvoiceClosingDay.GetValueOrDefault(1),
                firstScheduledMonthOffset + (installmentNumber - firstInstallmentNumberToSchedule));

            await _unitOfWork.RecurringTransactions.AddAsync(new RecurringTransaction
            {
                UserId = userId,
                AccountId = request.AccountId,
                CategoryId = request.CategoryId,
                Type = TransactionType.Expense,
                Amount = installmentAmounts[installmentNumber - 1],
                Description = BuildInstallmentDescription(request.Description, installmentNumber, request.InstallmentCount),
                Frequency = RecurrenceFrequency.Monthly,
                StartDate = scheduledDate,
                NextOccurrenceDate = scheduledDate,
                Tags = request.Tags,
                Notes = request.Notes,
                SkipAccountBalanceImpact = true,
                SkipCommittedCreditImpact = true,
                SkipCreditLimitValidation = true,
                IsInstallmentSchedule = true,
                InstallmentGroupId = installmentGroupId,
                InstallmentNumber = installmentNumber,
                InstallmentCount = request.InstallmentCount,
                RemainingOccurrences = 1
            });
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetAllAsync(string userId)
    {
        var transactions = await _unitOfWork.Transactions.GetAllAsync();
        return await MapToDtosAsync(
            userId,
            transactions
            .Where(t => t.UserId == userId && !t.IsDeleted)
            .ToList());
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
        transaction.Type = request.Type;
        transaction.Amount = request.Amount;
        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Tags = request.Tags;
        transaction.Notes = request.Notes;
        transaction.ToAccountId = request.ToAccountId;
        transaction.Status = request.Status;
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
        }
        else if (!string.IsNullOrEmpty(oldInvoiceId))
        {
            transaction.InvoiceId = null;
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        var invoiceIdsToRecalculate = new HashSet<string>(StringComparer.Ordinal);

        if (!string.IsNullOrWhiteSpace(oldInvoiceId))
        {
            invoiceIdsToRecalculate.Add(oldInvoiceId);
        }

        if (!string.IsNullOrWhiteSpace(transaction.InvoiceId))
        {
            invoiceIdsToRecalculate.Add(transaction.InvoiceId);
        }

        if (!string.IsNullOrEmpty(oldInvoiceId) && !string.Equals(oldInvoiceId, transaction.InvoiceId, StringComparison.Ordinal))
        {
            _processLogger.AddStep("Transaction moved between invoices", new Dictionary<string, object?>
            {
                ["oldInvoice"] = oldInvoiceId,
                ["newInvoice"] = transaction.InvoiceId
            });
        }

        foreach (var invoiceId in invoiceIdsToRecalculate)
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
        }

        _processLogger.AddStep("Transaction updated successfully");
        return await MapToDtoAsync(userId, transaction, newAccount);
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
        await _unitOfWork.SaveChangesAsync();

        if (!string.IsNullOrEmpty(invoiceId))
        {
            await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);
            _processLogger.AddStep("Invoice recalculated after deletion", new Dictionary<string, object?>
            {
                ["invoiceId"] = invoiceId
            });
        }

        _processLogger.AddStep("Transaction deleted (soft delete)");
    }

    private async Task ApplyTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.SkipAccountBalanceImpact)
        {
            return;
        }

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
                var destImpact = toAccount.Type == AccountType.CreditCard
                    ? CalculateCreditCardPaymentBalanceDelta(toAccount.Balance, transaction.Amount)
                    : transaction.Amount;

                sourceAccount.Balance += sourceImpact;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance += destImpact;
                    if (toAccount.Type == AccountType.CreditCard)
                    {
                        toAccount.CommittedCredit = Math.Max(0m, GetCommittedCreditSnapshot(toAccount) - transaction.Amount);
                    }
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

            if (!transaction.SkipCreditLimitValidation &&
                account.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense)
            {
                ValidateCreditLimit(account, transaction.Amount);
            }

            var currentCommittedCredit = account.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense
                ? GetCommittedCreditSnapshot(account)
                : 0m;

            var impactAmount = CalculateBalanceImpact(account.Type, transaction.Type, transaction.Amount);
            account.Balance += impactAmount;
            if (!transaction.SkipCommittedCreditImpact &&
                account.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense)
            {
                account.CommittedCredit = currentCommittedCredit + transaction.Amount;
            }
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }
    }

    private async Task RevertTransactionImpact(string userId, Transaction transaction)
    {
        if (transaction.SkipAccountBalanceImpact)
        {
            return;
        }

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
                var destRevert = toAccount.Type == AccountType.CreditCard
                    ? -CalculateCreditCardPaymentBalanceDelta(toAccount.Balance, transaction.Amount)
                    : -transaction.Amount;

                sourceAccount.Balance += sourceRevert;
                sourceAccount.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(sourceAccount);

                try
                {
                    toAccount.Balance += destRevert;
                    if (toAccount.Type == AccountType.CreditCard)
                    {
                        toAccount.CommittedCredit = GetCommittedCreditSnapshot(toAccount) + transaction.Amount;
                    }
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

            var currentCommittedCredit = account.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense
                ? GetCommittedCreditSnapshot(account)
                : 0m;

            var impactAmount = -CalculateBalanceImpact(account.Type, transaction.Type, transaction.Amount);
            account.Balance += impactAmount;
            if (!transaction.SkipCommittedCreditImpact &&
                account.Type == AccountType.CreditCard && transaction.Type == TransactionType.Expense)
            {
                account.CommittedCredit = Math.Max(0m, currentCommittedCredit - transaction.Amount);
            }
            account.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }
    }

    private static decimal GetCommittedCreditSnapshot(Account account)
    {
        if (account.Type != AccountType.CreditCard)
        {
            return 0m;
        }

        return Math.Max(account.CommittedCredit, Math.Abs(account.Balance));
    }

    private static decimal CalculateCreditCardPaymentBalanceDelta(decimal currentBalance, decimal amount)
    {
        return currentBalance >= 0m ? -amount : amount;
    }

    private static void ValidateCreditLimit(Account account, decimal requestedAmount)
    {
        if (account.Type != AccountType.CreditCard || !account.CreditLimit.HasValue)
        {
            return;
        }

        var currentCommittedCredit = GetCommittedCreditSnapshot(account);
        var newCommittedCredit = currentCommittedCredit + requestedAmount;

        if (newCommittedCredit <= account.CreditLimit.Value)
        {
            return;
        }

        var available = Math.Max(0m, account.CreditLimit.Value - currentCommittedCredit);
        throw new InvalidOperationException(
            $"Limite de crédito excedido. Disponível: R$ {available:F2} | Tentando usar: R$ {requestedAmount:F2}");
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

    private static List<decimal> CalculateInstallmentAmounts(decimal totalAmount, int installmentCount)
    {
        var roundedInstallment = Math.Round(totalAmount / installmentCount, 2, MidpointRounding.AwayFromZero);
        var amounts = Enumerable.Repeat(roundedInstallment, installmentCount).ToList();
        var difference = totalAmount - amounts.Sum();
        amounts[^1] += difference;
        return amounts;
    }

    private static DateTime CalculateInstallmentPostingDate(DateTime purchaseDate, int closingDay, int monthOffset)
    {
        var normalizedClosingDay = Math.Clamp(closingDay, 1, 31);
        var postingDay = normalizedClosingDay + 1;
        if (postingDay > 31)
        {
            postingDay = 1;
        }

        var targetMonth = purchaseDate.AddMonths(monthOffset);
        var day = Math.Min(postingDay, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));

        return new DateTime(targetMonth.Year, targetMonth.Month, day);
    }

    private static string BuildInstallmentDescription(string description, int installmentNumber, int installmentCount)
    {
        return $"{description} ({installmentNumber}/{installmentCount})";
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
            InstallmentGroupId = transaction.InstallmentGroupId,
            InstallmentNumber = transaction.InstallmentNumber,
            InstallmentCount = transaction.InstallmentCount,
            Status = transaction.Status,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
