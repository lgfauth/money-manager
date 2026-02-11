using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Application.Services;

/// <summary>
/// Implementação do serviço de gerenciamento de faturas de cartão de crédito
/// </summary>
public class CreditCardInvoiceService : ICreditCardInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreditCardInvoiceService> _logger;

    public CreditCardInvoiceService(
        IUnitOfWork unitOfWork,
        ILogger<CreditCardInvoiceService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    // ==================== GESTÃO DE FATURAS ====================

    public async Task<CreditCardInvoice> GetOrCreateOpenInvoiceAsync(string userId, string accountId)
    {
        _logger.LogDebug("Getting or creating open invoice for account {AccountId}", accountId);

        // Verificar se já existe uma fatura aberta
        var openInvoice = await _unitOfWork.CreditCardInvoices.GetOpenInvoiceByAccountIdAsync(accountId);
        if (openInvoice != null)
        {
            _logger.LogDebug("Found existing open invoice {InvoiceId}", openInvoice.Id);
            return openInvoice;
        }

        // Buscar informações do cartão
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
        if (account == null || account.UserId != userId)
            throw new KeyNotFoundException("Account not found");

        if (account.Type != AccountType.CreditCard)
            throw new InvalidOperationException("Account is not a credit card");

        _logger.LogInformation("Creating new open invoice for account {AccountId}", accountId);

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
        _logger.LogInformation("Closing invoice {InvoiceId} manually", invoiceId);

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
            account.LastInvoiceClosedAt = DateTime.UtcNow;
            await _unitOfWork.Accounts.UpdateAsync(account);
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Invoice {InvoiceId} closed successfully", invoiceId);

        return await MapToDtoAsync(invoice);
    }

    public async Task ProcessMonthlyInvoiceClosuresAsync()
    {
        var today = DateTime.UtcNow.Date;
        _logger.LogInformation("Processing monthly invoice closures for date: {Date}", today);

        // Buscar todos os cartões de crédito
        var allAccounts = await _unitOfWork.Accounts.GetAllAsync();
        var creditCards = allAccounts.Where(a => a.Type == AccountType.CreditCard && !a.IsDeleted).ToList();

        _logger.LogInformation("Found {Count} credit card accounts", creditCards.Count);

        var closedCount = 0;

        foreach (var card in creditCards)
        {
            try
            {
                var closingDay = card.InvoiceClosingDay ?? 1;
                
                // Verificar se hoje é o dia de fechamento
                if (today.Day != closingDay)
                    continue;

                _logger.LogDebug("Processing invoice closure for card {CardId} (closing day: {Day})", 
                    card.Id, closingDay);

                // Buscar fatura aberta
                var openInvoice = await _unitOfWork.CreditCardInvoices.GetOpenInvoiceByAccountIdAsync(card.Id);
                if (openInvoice == null)
                {
                    _logger.LogWarning("No open invoice found for card {CardId}, skipping", card.Id);
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
                card.LastInvoiceClosedAt = DateTime.UtcNow;
                await _unitOfWork.Accounts.UpdateAsync(card);

                closedCount++;

                _logger.LogInformation("Invoice {InvoiceId} closed for card {CardId}, new invoice {NewInvoiceId} created",
                    openInvoice.Id, card.Id, newInvoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invoice closure for card {CardId}", card.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Monthly invoice closure completed. {Count} invoices closed", closedCount);
    }

    // ==================== PAGAMENTO DE FATURAS ====================

    public async Task PayInvoiceAsync(string userId, PayInvoiceRequestDto request)
    {
        _logger.LogInformation("Processing full payment for invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(request.InvoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid");

        // Validar valor do pagamento
        if (request.Amount != invoice.RemainingAmount)
            throw new InvalidOperationException($"Payment amount must be equal to remaining amount (R$ {invoice.RemainingAmount:F2})");

        await ProcessPaymentAsync(invoice, request, true);
    }

    public async Task PayPartialInvoiceAsync(string userId, PayInvoiceRequestDto request)
    {
        _logger.LogInformation("Processing partial payment for invoice {InvoiceId}", request.InvoiceId);

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(request.InvoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid");

        if (request.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero");

        if (request.Amount > invoice.RemainingAmount)
            throw new InvalidOperationException($"Payment amount cannot exceed remaining amount (R$ {invoice.RemainingAmount:F2})");

        await ProcessPaymentAsync(invoice, request, false);
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

        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        var invoiceTransactions = allTransactions
            .Where(t => t.InvoiceId == invoiceId && !t.IsDeleted)
            .OrderBy(t => t.Date)
            .ToList();

        // Mapear para DTO (assumindo que TransactionResponseDto existe)
        return invoiceTransactions.Select(t => new TransactionResponseDto
        {
            Id = t.Id,
            AccountId = t.AccountId,
            CategoryId = t.CategoryId,
            Type = (int)t.Type,
            Amount = t.Amount,
            Date = t.Date,
            Description = t.Description,
            Tags = t.Tags,
            Status = (int)t.Status,
            CreatedAt = t.CreatedAt
        });
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
        _logger.LogInformation("Creating new invoice for account {AccountId} with closing date {ClosingDate}",
            accountId, targetClosingDate);

        return await CreateInvoiceForClosingDateAsync(account, targetClosingDate);
    }

    public async Task RecalculateInvoiceTotalAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId)
            throw new KeyNotFoundException("Invoice not found");

        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        var invoiceTransactions = allTransactions
            .Where(t => t.InvoiceId == invoiceId && !t.IsDeleted && t.Type == TransactionType.Expense)
            .ToList();

        var total = invoiceTransactions.Sum(t => t.Amount);

        invoice.TotalAmount = total;
        invoice.RemainingAmount = total - invoice.PaidAmount;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug("Invoice {InvoiceId} total recalculated: {Total} ({Count} transactions)",
            invoiceId, total, invoiceTransactions.Count);
    }

    public async Task<CreditCardInvoice> CreateHistoryInvoiceAsync(string userId, string accountId)
    {
        _logger.LogInformation("Creating history invoice for account {AccountId}", accountId);

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

        _logger.LogInformation("History invoice created for account {AccountId} with {Count} transactions",
            accountId, oldTransactions.Count);

        return historyInvoice;
    }

    // ==================== MÉTODOS PRIVADOS ====================

    private async Task<CreditCardInvoice> CreateNewOpenInvoiceAsync(Account account)
    {
        var today = DateTime.Today;
        var closingDay = account.InvoiceClosingDay ?? 1;

        // Calcular próximo fechamento
        DateTime nextClosing;
        if (today.Day <= closingDay)
        {
            // Próximo fechamento é este mês
            nextClosing = new DateTime(today.Year, today.Month, Math.Min(closingDay, DateTime.DaysInMonth(today.Year, today.Month)));
        }
        else
        {
            // Próximo fechamento é mês que vem
            var nextMonth = today.AddMonths(1);
            nextClosing = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(closingDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
        }

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

    private async Task ProcessPaymentAsync(CreditCardInvoice invoice, PayInvoiceRequestDto request, bool isFullPayment)
    {
        // Validar conta pagadora existe
        var payFromAccount = await _unitOfWork.Accounts.GetByIdAsync(request.PayFromAccountId);
        if (payFromAccount == null)
            throw new KeyNotFoundException("Payment account not found");

        if (payFromAccount.Type == AccountType.CreditCard)
            throw new InvalidOperationException("Cannot pay invoice from a credit card");

        // Atualizar fatura
        invoice.PaidAmount += request.Amount;
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (invoice.RemainingAmount <= 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Payment processed for invoice {InvoiceId}: R$ {Amount} (Remaining: R$ {Remaining})",
            invoice.Id, request.Amount, invoice.RemainingAmount);
        
        _logger.LogInformation("?? Note: Payment transaction must be created separately by calling TransactionService.CreateAsync()");
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
        var allTransactions = await _unitOfWork.Transactions.GetAllAsync();
        dto.TransactionCount = allTransactions.Count(t => t.InvoiceId == invoice.Id && !t.IsDeleted);

        return dto;
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
