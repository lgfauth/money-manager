using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de faturas de cartão de crédito
/// </summary>
public class CreditCardInvoiceService : ICreditCardInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProcessLogger _processLogger;

    public CreditCardInvoiceService(
        IUnitOfWork unitOfWork,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _processLogger = processLogger;
    }

    // ==================== GESTÃO DE FATURAS ====================

    public async Task<CreditCardInvoice> GetOrCreateOpenInvoiceAsync(string userId, string accountId)
    {
        _processLogger.AddStep("Getting or creating open invoice", new Dictionary<string, object?> { ["accountId"] = accountId });

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        if (account.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Account is not a credit card");

        var expectedClosingDate = CalculateNextClosingDate(DateTime.Today, account);
        var expectedReferenceMonth = expectedClosingDate.ToString("yyyy-MM");

        // Verificar se já existe uma fatura aberta
        var openInvoice = await _unitOfWork.CreditCardInvoices.GetOpenInvoiceByAccountIdAsync(accountId);
        if (openInvoice != null)
        {
            if (string.Equals(openInvoice.ReferenceMonth, expectedReferenceMonth, StringComparison.Ordinal))
            {
                _processLogger.AddStep("Found existing open invoice", new Dictionary<string, object?> { ["invoiceId"] = openInvoice.Id });
                return openInvoice;
            }

            _processLogger.AddWarning("Found stale open invoice, normalizing before returning current invoice", new Dictionary<string, object?>
            {
                ["invoiceId"] = openInvoice.Id,
                ["staleReferenceMonth"] = openInvoice.ReferenceMonth,
                ["expectedReferenceMonth"] = expectedReferenceMonth
            });

            await NormalizeStaleOpenInvoiceAsync(userId, account, openInvoice);

            var normalizedInvoice = await _unitOfWork.CreditCardInvoices.GetByReferenceMonthAsync(accountId, expectedReferenceMonth);
            if (normalizedInvoice != null)
            {
                _processLogger.AddStep("Using normalized open invoice", new Dictionary<string, object?> { ["invoiceId"] = normalizedInvoice.Id });
                return normalizedInvoice;
            }

            _processLogger.AddStep("Found existing open invoice", new Dictionary<string, object?> { ["invoiceId"] = openInvoice.Id });
        }

        var expectedInvoice = await _unitOfWork.CreditCardInvoices.GetByReferenceMonthAsync(accountId, expectedReferenceMonth);
        if (expectedInvoice != null)
        {
            if (expectedInvoice.Status != InvoiceStatus.Open)
            {
                expectedInvoice.Status = InvoiceStatus.Open;
                expectedInvoice.ClosedAt = null;
                expectedInvoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(expectedInvoice);

                account.CurrentOpenInvoiceId = expectedInvoice.Id;
                await _unitOfWork.Accounts.UpdateAsync(account);
                await _unitOfWork.SaveChangesAsync();
            }

            return expectedInvoice;
        }

        _processLogger.AddStep("Creating new open invoice", new Dictionary<string, object?> { ["accountId"] = accountId });

        // Criar nova fatura aberta
        var invoice = await CreateNewOpenInvoiceAsync(account);

        // Atualizar referência no cartão
        account.CurrentOpenInvoiceId = invoice.Id;
        await _unitOfWork.Accounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<CreditCardInvoiceResponseDto> GetInvoiceByIdAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        return await MapToDtoAsync(invoice);
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetInvoicesByAccountAsync(string userId, string accountId)
    {
        var invoices = await _unitOfWork.CreditCardInvoices.GetByAccountIdAsync(accountId);
        
        var result = new List<CreditCardInvoiceResponseDto>();
        foreach (var invoice in invoices)
        {
            if (invoice.UserId == userId)
            {
                result.Add(await MapToDtoAsync(invoice));
            }
        }

        return result;
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetPendingInvoicesAsync(string userId)
    {
        var invoices = await _unitOfWork.CreditCardInvoices.GetClosedUnpaidInvoicesAsync(userId);
        
        var result = new List<CreditCardInvoiceResponseDto>();
        foreach (var invoice in invoices)
        {
            result.Add(await MapToDtoAsync(invoice));
        }

        return result;
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetOverdueInvoicesAsync(string userId)
    {
        var invoices = await _unitOfWork.CreditCardInvoices.GetOverdueInvoicesAsync(userId);
        
        var result = new List<CreditCardInvoiceResponseDto>();
        foreach (var invoice in invoices)
        {
            result.Add(await MapToDtoAsync(invoice));
        }

        return result;
    }

    // ==================== FECHAMENTO DE FATURAS ====================

    public async Task<CreditCardInvoiceResponseDto> CloseInvoiceAsync(string userId, string invoiceId)
    {
        _processLogger.AddStep("Closing invoice manually", new Dictionary<string, object?> { ["invoiceId"] = invoiceId });

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status != InvoiceStatus.Open)
            throw new InvalidOperationException($"Invoice is not open (current status: {invoice.Status})");

        // Recalcular total antes de fechar
        await RecalculateInvoiceTotalAsync(userId, invoiceId);

        // Fechar fatura
        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAt = DateTime.UtcNow;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);

        // Buscar o cartão e criar nova fatura aberta
        var account = await _unitOfWork.Accounts.GetByIdAsync(invoice.AccountId);
        if (account != null)
        {
            var newInvoice = await CreateNewOpenInvoiceAsync(account);
            account.CurrentOpenInvoiceId = newInvoice.Id;
            account.LastInvoiceClosedAt = invoice.PeriodEnd.Date;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Invoice closed successfully", new Dictionary<string, object?> { ["invoiceId"] = invoiceId });

        return await MapToDtoAsync(invoice);
    }

    public async Task ProcessMonthlyInvoiceClosuresAsync(DateTime referenceDate)
    {
        var today = referenceDate.Date;
        _processLogger.AddStep("Processing monthly invoice closures", new Dictionary<string, object?> { ["date"] = today.ToString("O") });

        // Buscar todos os cartões de crédito
        var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
        var creditCards = allAccounts.Where(a => a.Type == AccountType.CreditCard && !a.IsDeleted).ToList();

        _processLogger.AddStep("Credit card accounts found", new Dictionary<string, object?> { ["count"] = creditCards.Count });

        var closedCount = 0;

        foreach (var card in creditCards)
        {
            try
            {
                var closingDay = card.InvoiceClosingDay ?? 1;
                var effectiveClosingDay = GetEffectiveClosingDay(closingDay, today.Year, today.Month);

                if (effectiveClosingDay != closingDay)
                {
                    _processLogger.AddStep("Adjusted configured closing day for short month", new Dictionary<string, object?>
                    {
                        ["cardId"] = card.Id,
                        ["configuredClosingDay"] = closingDay,
                        ["effectiveClosingDay"] = effectiveClosingDay,
                        ["year"] = today.Year,
                        ["month"] = today.Month
                    });
                }
                
                // Verificar se hoje é o dia de fechamento
                if (today.Day != effectiveClosingDay)
                    continue;

                _processLogger.AddStep("Processing invoice closure", new Dictionary<string, object?>
                {
                    ["cardId"] = card.Id,
                    ["closingDay"] = closingDay,
                    ["effectiveClosingDay"] = effectiveClosingDay
                });

                // Buscar fatura aberta
                var openInvoice = await _unitOfWork.CreditCardInvoices.GetOpenInvoiceByAccountIdAsync(card.Id);
                if (openInvoice == null)
                {
                    _processLogger.AddWarning("No open invoice found, skipping", new Dictionary<string, object?> { ["cardId"] = card.Id });
                    continue;
                }

                // Recalcular total
                await RecalculateInvoiceTotalAsync(card.UserId, openInvoice.Id);

                // Fechar fatura
                openInvoice.Status = InvoiceStatus.Closed;
                openInvoice.ClosedAt = DateTime.UtcNow;
                openInvoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(openInvoice);

                // Criar nova fatura aberta
                var newInvoice = await CreateNewOpenInvoiceAsync(card);

                // Atualizar cartão
                card.CurrentOpenInvoiceId = newInvoice.Id;
                card.LastInvoiceClosedAt = openInvoice.PeriodEnd.Date;
                await _unitOfWork.Accounts.UpdateAsync(card);

                closedCount++;

                _processLogger.AddStep("Invoice closed, new invoice created", new Dictionary<string, object?> { ["closedInvoiceId"] = openInvoice.Id, ["cardId"] = card.Id, ["newInvoiceId"] = newInvoice.Id });
            }
            catch (Exception ex)
            {
                _processLogger.AddError($"Error processing invoice closure for card {card.Id}", ex);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Monthly invoice closure completed", new Dictionary<string, object?> { ["closedCount"] = closedCount });
    }

    public async Task<int> MarkOverdueInvoicesAsync(DateTime? referenceDate = null)
    {
        var today = (referenceDate?.Date ?? DateTime.UtcNow.Date);
        var invoices = await _unitOfWork.CreditCardInvoices.GetAllAsync();

        var overdueCandidates = invoices
            .Where(invoice => !invoice.IsDeleted
                && invoice.DueDate.Date < today
                && (invoice.Status == InvoiceStatus.Closed || invoice.Status == InvoiceStatus.PartiallyPaid))
            .ToList();

        if (overdueCandidates.Count == 0)
        {
            _processLogger.AddStep("No overdue invoices found", new Dictionary<string, object?> { ["referenceDate"] = today.ToString("O") });
            return 0;
        }

        foreach (var invoice in overdueCandidates)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        }

        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Overdue invoices updated", new Dictionary<string, object?>
        {
            ["referenceDate"] = today.ToString("O"),
            ["updatedCount"] = overdueCandidates.Count
        });

        return overdueCandidates.Count;
    }

    // ==================== PAGAMENTO DE FATURAS ====================

    public async Task PayInvoiceAsync(string userId, PayInvoiceRequestDto request)
    {
        _processLogger.AddStep("Processing full payment", new Dictionary<string, object?> { ["invoiceId"] = request.InvoiceId });

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(request.InvoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid");

        // Validar valor do pagamento
        if (request.Amount != invoice.RemainingAmount)
            throw new InvalidOperationException($"Payment amount must be equal to remaining amount (R$ {invoice.RemainingAmount:F2})");

        await ProcessPaymentAsync(userId, invoice, request, true);
    }

    public async Task PayPartialInvoiceAsync(string userId, PayInvoiceRequestDto request)
    {
        _processLogger.AddStep("Processing partial payment", new Dictionary<string, object?> { ["invoiceId"] = request.InvoiceId });

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(request.InvoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero");

        if (request.Amount > invoice.RemainingAmount)
            throw new InvalidOperationException($"Payment amount cannot exceed remaining amount (R$ {invoice.RemainingAmount:F2})");

        await ProcessPaymentAsync(userId, invoice, request, false);
    }

    // ==================== RELATÓRIOS ====================

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        var transactions = await GetInvoiceTransactionsAsync(userId, invoiceId);
        
        var summary = new InvoiceSummaryDto
        {
            Invoice = await MapToDtoAsync(invoice),
            Transactions = transactions.ToList(),
            TotalTransactions = transactions.Count(),
            AverageTransactionAmount = transactions.Any() ? transactions.Average(t => t.Amount) : 0,
            AmountByCategory = transactions
                .Where(t => !string.IsNullOrEmpty(t.CategoryId))
                .GroupBy(t => t.CategoryId)
                .ToDictionary(g => g.Key ?? "Sem Categoria", g => g.Sum(t => t.Amount))
        };

        return summary;
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetInvoiceTransactionsAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        var invoiceTransactions = await GetEligibleInvoiceTransactionsAsync(invoiceId);
        var orderedTransactions = invoiceTransactions
            .OrderBy(t => t.Date)
            .ToList();

        return await MapTransactionsToDtoAsync(userId, orderedTransactions);
    }

    // ==================== UTILITÁRIOS ====================

    public async Task<CreditCardInvoice> DetermineInvoiceForTransactionAsync(string userId, string accountId, DateTime transactionDate)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        if (account.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Account is not a credit card");

        var closingDay = account.InvoiceClosingDay ?? 1;
        var transactionDateOnly = transactionDate.Date;

        // Calcular data de fechamento do mês da transação
        var closingDateThisMonth = new DateTime(
            transactionDateOnly.Year,
            transactionDateOnly.Month,
            Math.Min(closingDay, DateTime.DaysInMonth(transactionDateOnly.Year, transactionDateOnly.Month))
        );

        // Se a transação é até o dia de fechamento, vai para a fatura que fecha neste mês
        // Se é depois, vai para a fatura que fecha no próximo mês
        DateTime targetClosingDate;
        if (transactionDateOnly <= closingDateThisMonth)
        {
            targetClosingDate = closingDateThisMonth;
        }
        else
        {
            var nextMonth = transactionDateOnly.AddMonths(1);
            targetClosingDate = new DateTime(
                nextMonth.Year,
                nextMonth.Month,
                Math.Min(closingDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month))
            );
        }

        // Buscar fatura para este período de fechamento
        var referenceMonth = targetClosingDate.ToString("yyyy-MM");
        var invoice = await _unitOfWork.CreditCardInvoices.GetByReferenceMonthAsync(accountId, referenceMonth);

        if (invoice != null)
            return invoice;

        // Se não existe, criar nova fatura
        _processLogger.AddStep("Creating new invoice for closing date", new Dictionary<string, object?> { ["accountId"] = accountId, ["closingDate"] = targetClosingDate.ToString("O") });

        return await CreateInvoiceForClosingDateAsync(account, targetClosingDate);
    }

    public async Task RecalculateInvoiceTotalAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        var invoiceTransactions = await GetEligibleInvoiceTransactionsAsync(invoiceId);

        var total = invoiceTransactions.Sum(t => t.Amount);

        invoice.TotalAmount = total;
        invoice.RemainingAmount = Math.Max(0m, total - invoice.PaidAmount);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Invoice total recalculated", new Dictionary<string, object?> { ["invoiceId"] = invoiceId, ["total"] = total, ["transactionCount"] = invoiceTransactions.Count });
    }

    public async Task<CreditCardInvoice> CreateHistoryInvoiceAsync(string userId, string accountId)
    {
        _processLogger.AddStep("Creating history invoice", new Dictionary<string, object?> { ["accountId"] = accountId });

        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        if (account.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Account is not a credit card");

        // Criar fatura "Histórico" de 2020 até ontem
        var yesterday = DateTime.Today.AddDays(-1);
        var historyStart = new DateTime(2020, 1, 1);

        var historyInvoice = new CreditCardInvoice
        {
            AccountId = accountId,
            UserId = userId,
            PeriodStart = historyStart,
            PeriodEnd = yesterday,
            DueDate = yesterday,
            TotalAmount = Math.Abs(account.Balance), // Dívida atual
            PaidAmount = Math.Abs(account.Balance),   // Marcar como paga
            RemainingAmount = 0,
            Status = InvoiceStatus.Paid,
            ClosedAt = yesterday,
            PaidAt = yesterday,
            ReferenceMonth = "HISTORY",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CreditCardInvoices.AddAsync(historyInvoice);

        // Vincular todas as transações antigas a esta fatura
        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        var oldTransactions = allTransactions
            .Where(t => t.AccountId == accountId && t.Date <= yesterday && string.IsNullOrEmpty(t.InvoiceId))
            .ToList();

        foreach (var transaction in oldTransactions)
        {
            transaction.InvoiceId = historyInvoice.Id;
            await _unitOfWork.Transactions.UpdateAsync(transaction);
        }

        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("History invoice created", new Dictionary<string, object?> { ["accountId"] = accountId, ["transactionCount"] = oldTransactions.Count });

        return historyInvoice;
    }

    public async Task<CreditCardReconciliationSummaryDto> ReconcileCreditCardDataAsync(string userId)
    {
        var accounts = await _unitOfWork.Accounts.GetAllAsync();
        var creditCards = accounts
            .Where(account => account.UserId == userId && !account.IsDeleted && account.Type == AccountType.CreditCard)
            .ToList();

        var summary = new CreditCardReconciliationSummaryDto();

        foreach (var card in creditCards)
        {
            summary.AccountsProcessed++;

            await EnsureCurrentOpenInvoiceAsync(userId, card);

            var invoices = (await _unitOfWork.CreditCardInvoices.GetByAccountIdAsync(card.Id))
                .Where(invoice => !invoice.IsDeleted)
                .ToList();

            foreach (var invoice in invoices)
            {
                await RecalculateInvoiceTotalAsync(userId, invoice.Id);
                summary.InvoicesRecalculated++;
            }

            invoices = (await _unitOfWork.CreditCardInvoices.GetByAccountIdAsync(card.Id))
                .Where(invoice => !invoice.IsDeleted)
                .ToList();

            var activeInstallmentSchedules = (await _unitOfWork.RecurringTransactions.GetAllAsync())
                .Where(recurrence => recurrence.UserId == userId
                    && !recurrence.IsDeleted
                    && recurrence.IsActive
                    && recurrence.IsInstallmentSchedule
                    && recurrence.AccountId == card.Id)
                .ToList();

            var unpaidInvoicesTotal = invoices
                .Where(invoice => invoice.Status != InvoiceStatus.Paid)
                .Sum(invoice => Math.Max(0m, invoice.RemainingAmount));

            var scheduledInstallmentsTotal = activeInstallmentSchedules.Sum(recurrence => recurrence.Amount);
            var recomputedCommittedCredit = Math.Max(Math.Abs(card.Balance), unpaidInvoicesTotal + scheduledInstallmentsTotal);

            card.CommittedCredit = recomputedCommittedCredit;
            card.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(card);

            summary.AccountsUpdated++;
            summary.TotalCommittedCredit += recomputedCommittedCredit;
        }

        await _unitOfWork.SaveChangesAsync();

        return summary;
    }

    // ==================== MÉTODOS PRIVADOS ====================

    private async Task<CreditCardInvoice> CreateNewOpenInvoiceAsync(Account account)
    {
        var today = DateTime.Today;
        var nextClosing = CalculateNextClosingDate(today, account);

        var periodStart = account.LastInvoiceClosedAt?.Date.AddDays(1) ?? today;
        var dueDate = nextClosing.AddDays(account.InvoiceDueDayOffset);

        var invoice = new CreditCardInvoice
        {
            AccountId = account.Id,
            UserId = account.UserId,
            PeriodStart = periodStart,
            PeriodEnd = nextClosing,
            DueDate = dueDate,
            TotalAmount = 0,
            PaidAmount = 0,
            RemainingAmount = 0,
            Status = InvoiceStatus.Open,
            ReferenceMonth = nextClosing.ToString("yyyy-MM"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CreditCardInvoices.AddAsync(invoice);

        return invoice;
    }

    private async Task<List<TransactionResponseDto>> MapTransactionsToDtoAsync(string userId, IReadOnlyCollection<Transaction> transactions)
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

        return transactions.Select(transaction =>
        {
            accounts.TryGetValue(transaction.AccountId, out var account);
            var category = transaction.CategoryId != null && categories.TryGetValue(transaction.CategoryId, out var resolvedCategory)
                ? resolvedCategory
                : null;

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
        }).ToList();
    }

    private async Task<CreditCardInvoice> CreateInvoiceForClosingDateAsync(Account account, DateTime closingDate)
    {
        var periodStart = account.LastInvoiceClosedAt?.Date.AddDays(1) ?? closingDate.AddMonths(-1).AddDays(1);
        var dueDate = closingDate.AddDays(account.InvoiceDueDayOffset);

        var invoice = new CreditCardInvoice
        {
            AccountId = account.Id,
            UserId = account.UserId,
            PeriodStart = periodStart,
            PeriodEnd = closingDate,
            DueDate = dueDate,
            TotalAmount = 0,
            PaidAmount = 0,
            RemainingAmount = 0,
            Status = closingDate <= DateTime.Today ? InvoiceStatus.Closed : InvoiceStatus.Open,
            ClosedAt = closingDate <= DateTime.Today ? closingDate : null,
            ReferenceMonth = closingDate.ToString("yyyy-MM"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CreditCardInvoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    private async Task ProcessPaymentAsync(string userId, CreditCardInvoice invoice, PayInvoiceRequestDto request, bool isFullPayment)
    {
        // Validar conta pagadora existe e pertence ao usuário
        var payFromAccount = await _unitOfWork.Accounts.GetByIdAsync(request.PayFromAccountId);
        if (payFromAccount == null || payFromAccount.UserId != userId || payFromAccount.IsDeleted)
            throw new KeyNotFoundException("Payment account not found");

        if (payFromAccount.Type == AccountType.CreditCard)
            throw new InvalidOperationException("Cannot pay invoice from a credit card");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero");

        if (payFromAccount.Balance < request.Amount)
            throw new InvalidOperationException("Insufficient balance in payment account");

        var creditCardAccount = await _unitOfWork.Accounts.GetByIdAsync(invoice.AccountId);
        if (creditCardAccount == null || creditCardAccount.UserId != userId || creditCardAccount.IsDeleted)
            throw new KeyNotFoundException("Credit card account not found");

        if (creditCardAccount.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Invoice account is not a credit card");

        // Atualizar fatura
        invoice.PaidAmount += request.Amount;
        invoice.RemainingAmount = Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount);
        invoice.UpdatedAt = DateTime.UtcNow;

        if (invoice.RemainingAmount <= 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = request.PaymentDate;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
            invoice.PaidAt = null;
        }

        payFromAccount.Balance -= request.Amount;
        payFromAccount.UpdatedAt = DateTime.UtcNow;

        creditCardAccount.Balance += CalculateCreditCardPaymentBalanceDelta(creditCardAccount.Balance, request.Amount);
        creditCardAccount.CommittedCredit = Math.Max(0m, GetCommittedCreditSnapshot(creditCardAccount) - request.Amount);
        creditCardAccount.UpdatedAt = DateTime.UtcNow;

        var paymentTransaction = new Transaction
        {
            UserId = userId,
            AccountId = payFromAccount.Id,
            ToAccountId = creditCardAccount.Id,
            Type = TransactionType.Transfer,
            Amount = request.Amount,
            Currency = payFromAccount.Currency,
            Date = request.PaymentDate,
            Description = request.Description ?? $"Pagamento de fatura {invoice.ReferenceMonth}",
            Status = TransactionStatus.Completed
        };

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.Accounts.UpdateAsync(payFromAccount);
        await _unitOfWork.Accounts.UpdateAsync(creditCardAccount);
        await _unitOfWork.Transactions.AddAsync(paymentTransaction);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Payment processed", new Dictionary<string, object?> { ["invoiceId"] = invoice.Id, ["paidAmount"] = request.Amount, ["remaining"] = invoice.RemainingAmount });
    }

    private async Task EnsureCurrentOpenInvoiceAsync(string userId, Account account)
    {
        var openInvoice = await _unitOfWork.CreditCardInvoices.GetOpenInvoiceByAccountIdAsync(account.Id);

        if (openInvoice == null)
        {
            await GetOrCreateOpenInvoiceAsync(userId, account.Id);
            return;
        }

        var expectedReferenceMonth = CalculateNextClosingDate(DateTime.Today, account).ToString("yyyy-MM");
        if (!string.Equals(openInvoice.ReferenceMonth, expectedReferenceMonth, StringComparison.Ordinal))
        {
            await GetOrCreateOpenInvoiceAsync(userId, account.Id);
        }
    }

    private async Task NormalizeStaleOpenInvoiceAsync(string userId, Account account, CreditCardInvoice staleOpenInvoice)
    {
        await RecalculateInvoiceTotalAsync(userId, staleOpenInvoice.Id);

        staleOpenInvoice.Status = staleOpenInvoice.RemainingAmount > 0m
            ? InvoiceStatus.Closed
            : InvoiceStatus.Paid;
        staleOpenInvoice.ClosedAt = staleOpenInvoice.PeriodEnd.Date;
        staleOpenInvoice.UpdatedAt = DateTime.UtcNow;

        if (staleOpenInvoice.Status == InvoiceStatus.Paid && staleOpenInvoice.PaidAt == null)
        {
            staleOpenInvoice.PaidAt = staleOpenInvoice.PeriodEnd.Date;
        }

        await _unitOfWork.CreditCardInvoices.UpdateAsync(staleOpenInvoice);

        account.LastInvoiceClosedAt = staleOpenInvoice.PeriodEnd.Date;
        account.CurrentOpenInvoiceId = null;
        account.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Accounts.UpdateAsync(account);
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<CreditCardInvoiceResponseDto> MapToDtoAsync(CreditCardInvoice invoice)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(invoice.AccountId);
        var today = DateTime.Today;

        var dto = new CreditCardInvoiceResponseDto
        {
            Id = invoice.Id,
            AccountId = invoice.AccountId,
            AccountName = account?.Name ?? "Unknown",
            PeriodStart = invoice.PeriodStart,
            PeriodEnd = invoice.PeriodEnd,
            DueDate = invoice.DueDate,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            RemainingAmount = invoice.RemainingAmount,
            Status = invoice.Status,
            StatusLabel = GetStatusLabel(invoice.Status),
            ClosedAt = invoice.ClosedAt,
            PaidAt = invoice.PaidAt,
            ReferenceMonth = invoice.ReferenceMonth,
            IsOverdue = invoice.Status != InvoiceStatus.Paid && invoice.DueDate < today,
            DaysUntilDue = (invoice.DueDate - today).Days,
            CreatedAt = invoice.CreatedAt
        };

        // Contar transações
        dto.TransactionCount = (await GetEligibleInvoiceTransactionsAsync(invoice.Id)).Count;

        return dto;
    }

    private async Task<List<Transaction>> GetEligibleInvoiceTransactionsAsync(string invoiceId)
    {
        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();

        return allTransactions
            .Where(t => t.InvoiceId == invoiceId && !t.IsDeleted && t.Type == TransactionType.Expense)
            .ToList();
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

    private static DateTime CalculateNextClosingDate(DateTime currentDate, Account account)
    {
        var closingDay = account.InvoiceClosingDay ?? 1;

        if (currentDate.Day <= closingDay)
        {
            return new DateTime(
                currentDate.Year,
                currentDate.Month,
                Math.Min(closingDay, DateTime.DaysInMonth(currentDate.Year, currentDate.Month)));
        }

        var nextMonth = currentDate.AddMonths(1);
        return new DateTime(
            nextMonth.Year,
            nextMonth.Month,
            Math.Min(closingDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
    }

    private static int GetEffectiveClosingDay(int configuredClosingDay, int year, int month)
    {
        var normalizedClosingDay = Math.Clamp(configuredClosingDay, 1, 31);
        return Math.Min(normalizedClosingDay, DateTime.DaysInMonth(year, month));
    }

    private static string GetStatusLabel(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Open => "Aberta",
            InvoiceStatus.Closed => "Fechada",
            InvoiceStatus.Paid => "Paga",
            InvoiceStatus.PartiallyPaid => "Parcialmente Paga",
            InvoiceStatus.Overdue => "Vencida",
            _ => status.ToString()
        };
    }
}
