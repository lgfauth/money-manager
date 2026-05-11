using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface ICreditCardInvoiceService
{
    Task<IEnumerable<CreditCardInvoiceResponseDto>> GetByCardAsync(string userId, string creditCardId);
    Task<CreditCardInvoiceDetailResponseDto> GetDetailAsync(string userId, string invoiceId);
    Task<CreditCardInvoiceResponseDto> PayAsync(string userId, string invoiceId, PayCreditCardInvoiceRequestDto request);
    Task<CreditCardInvoiceResponseDto> OpenCurrentInvoiceAsync(string userId, string creditCardId);

    Task<CreditCardInvoice> EnsureCurrentOpenInvoiceAsync(string userId, CreditCard card);
    Task<CreditCardInvoice> GetOrCreateInvoiceAsync(string userId, CreditCard card, string referenceMonth, InvoiceStatus initialStatus);
    Task RecalculateTotalAsync(string userId, string invoiceId);
    Task<InvoiceStatusSummary> PromotePendingAndMarkOverdueAsync();
}

public record InvoiceStatusSummary(int PromotedToOpen, int ClosedInvoices, int MarkedOverdue);

public class CreditCardInvoiceService : ICreditCardInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITransactionService _transactionService;
    private readonly IProcessLogger _processLogger;

    public CreditCardInvoiceService(
        IUnitOfWork unitOfWork,
        ITransactionService transactionService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _transactionService = transactionService;
        _processLogger = processLogger;
    }

    public async Task<IEnumerable<CreditCardInvoiceResponseDto>> GetByCardAsync(string userId, string creditCardId)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(creditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        await EnsureCurrentOpenInvoiceAsync(userId, card);

        var invoices = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, creditCardId))
            .OrderByDescending(i => i.ReferenceMonth)
            .ToList();

        return invoices.Select(i => MapToDto(i, card));
    }

    public async Task<CreditCardInvoiceDetailResponseDto> GetDetailAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId || invoice.IsDeleted)
            throw new KeyNotFoundException("Invoice not found");

        var card = await _unitOfWork.CreditCards.GetByIdAsync(invoice.CreditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        var transactions = (await _unitOfWork.CreditCardTransactions.GetByInvoiceAsync(userId, invoiceId))
            .OrderBy(t => t.PurchaseDate)
            .ToList();

        var categoryIds = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.CategoryId))
            .Select(t => t.CategoryId!)
            .Distinct()
            .ToHashSet();

        var categories = (await _unitOfWork.Categories.GetAllAsync())
            .Where(c => c.UserId == userId && !c.IsDeleted && categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        return new CreditCardInvoiceDetailResponseDto
        {
            Invoice = MapToDto(invoice, card),
            Transactions = transactions.Select(t =>
            {
                Category? category = null;
                if (!string.IsNullOrWhiteSpace(t.CategoryId))
                {
                    categories.TryGetValue(t.CategoryId, out category);
                }
                return MapTransactionToDto(t, card, category);
            }).ToList()
        };
    }

    public async Task<CreditCardInvoiceResponseDto> PayAsync(string userId, string invoiceId, PayCreditCardInvoiceRequestDto request)
    {
        _processLogger.AddStep("Paying credit card invoice", new Dictionary<string, object?>
        {
            ["invoiceId"] = invoiceId,
            ["accountId"] = request.PaidWithAccountId,
            ["amount"] = request.PaidAmount
        });

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId || invoice.IsDeleted)
            throw new KeyNotFoundException("Invoice not found");

        if (invoice.Status != InvoiceStatus.Closed && invoice.Status != InvoiceStatus.Overdue)
            throw new InvalidOperationException("Only closed or overdue invoices can be paid");

        var account = await _unitOfWork.Accounts.GetByIdAsync(request.PaidWithAccountId);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new KeyNotFoundException("Account not found");

        var card = await _unitOfWork.CreditCards.GetByIdAsync(invoice.CreditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        var debitRequest = new CreateTransactionRequestDto
        {
            AccountId = request.PaidWithAccountId,
            CategoryId = null,
            Type = TransactionType.Expense,
            Amount = request.PaidAmount,
            Date = request.PaidAt,
            Description = $"Pagamento fatura {card.Name} ({invoice.ReferenceMonth})",
            Status = TransactionStatus.Completed
        };

        var debitTransaction = await _transactionService.CreateAsync(userId, debitRequest);

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = request.PaidAt;
        invoice.PaidWithAccountId = request.PaidWithAccountId;
        invoice.PaidAmount = request.PaidAmount;
        invoice.PaymentTransactionId = debitTransaction.Id;
        invoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Invoice paid", new Dictionary<string, object?>
        {
            ["invoiceId"] = invoiceId,
            ["paymentTransactionId"] = debitTransaction.Id
        });

        return MapToDto(invoice, card);
    }

    public async Task<CreditCardInvoice> EnsureCurrentOpenInvoiceAsync(string userId, CreditCard card)
    {
        var invoices = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, card.Id)).ToList();
        var today = DateTime.UtcNow.Date;

        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Pending).ToList())
        {
            var (refYear, refMonth) = CreditCardDateUtils.ParseReferenceMonth(invoice.ReferenceMonth);
            var firstDayOfReference = new DateTime(refYear, refMonth, 1);
            if (firstDayOfReference.Year < today.Year ||
                (firstDayOfReference.Year == today.Year && firstDayOfReference.Month <= today.Month))
            {
                invoice.Status = InvoiceStatus.Open;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
            }
        }

        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Open).ToList())
        {
            if (invoice.ClosingDate.Date <= today)
            {
                invoice.Status = InvoiceStatus.Closed;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
            }
        }

        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatus.Closed).ToList())
        {
            if (invoice.DueDate.Date < today)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
            }
        }

        var refreshed = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, card.Id)).ToList();
        var currentOpen = refreshed
            .Where(i => i.Status == InvoiceStatus.Open)
            .OrderBy(i => i.ReferenceMonth)
            .FirstOrDefault();

        if (currentOpen != null)
        {
            return currentOpen;
        }

        var referenceMonth = CreditCardDateUtils.ReferenceMonthForPurchaseDate(today, card.ClosingDay);

        // Verificar se há fatura pendente para o período de referência corrente e promovê-la
        var pendingForTarget = refreshed.FirstOrDefault(i =>
            i.ReferenceMonth == referenceMonth && i.Status == InvoiceStatus.Pending);

        if (pendingForTarget != null)
        {
            pendingForTarget.Status = InvoiceStatus.Open;
            pendingForTarget.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(pendingForTarget);
            await _unitOfWork.SaveChangesAsync();
            return pendingForTarget;
        }

        return await GetOrCreateInvoiceAsync(userId, card, referenceMonth, InvoiceStatus.Open);
    }

    public async Task<CreditCardInvoice> GetOrCreateInvoiceAsync(string userId, CreditCard card, string referenceMonth, InvoiceStatus initialStatus)
    {
        var existing = await _unitOfWork.CreditCardInvoices.GetByCardAndReferenceAsync(userId, card.Id, referenceMonth);
        if (existing != null)
        {
            return existing;
        }

        var invoice = new CreditCardInvoice
        {
            UserId = userId,
            CreditCardId = card.Id,
            ReferenceMonth = referenceMonth,
            ClosingDate = CreditCardDateUtils.ComputeClosingDate(referenceMonth, card.ClosingDay),
            DueDate = CreditCardDateUtils.ComputeDueDate(referenceMonth, card.ClosingDay, card.BillingDueDay),
            Status = initialStatus,
            TotalAmount = 0
        };

        await _unitOfWork.CreditCardInvoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<CreditCardInvoiceResponseDto> OpenCurrentInvoiceAsync(string userId, string creditCardId)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(creditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        var today = DateTime.UtcNow.Date;
        var referenceMonth = CreditCardDateUtils.ReferenceMonthForPurchaseDate(today, card.ClosingDay);

        var invoices = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, creditCardId)).ToList();

        // Se já existe fatura aberta para o período corrente, retornar sem alteração
        var existingOpen = invoices.FirstOrDefault(i =>
            i.Status == InvoiceStatus.Open && i.ReferenceMonth == referenceMonth);
        if (existingOpen != null)
            return MapToDto(existingOpen, card);

        // Se há fatura pendente para o período corrente, promover para corrente
        var pendingForPeriod = invoices.FirstOrDefault(i =>
            i.Status == InvoiceStatus.Pending && i.ReferenceMonth == referenceMonth);
        if (pendingForPeriod != null)
        {
            pendingForPeriod.Status = InvoiceStatus.Open;
            pendingForPeriod.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(pendingForPeriod);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(pendingForPeriod, card);
        }

        // Se já existe fatura em outro status para o período, não criar duplicata
        var existingForPeriod = invoices.FirstOrDefault(i =>
            i.ReferenceMonth == referenceMonth && !i.IsDeleted);
        if (existingForPeriod != null)
            throw new InvalidOperationException($"Já existe uma fatura para o período {referenceMonth} com status {existingForPeriod.Status}.");

        // Criar nova fatura corrente
        var newInvoice = await GetOrCreateInvoiceAsync(userId, card, referenceMonth, InvoiceStatus.Open);
        return MapToDto(newInvoice, card);
    }

    public async Task RecalculateTotalAsync(string userId, string invoiceId)
    {
        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(invoiceId);
        if (invoice == null || invoice.UserId != userId || invoice.IsDeleted)
            return;

        var transactions = await _unitOfWork.CreditCardTransactions.GetByInvoiceAsync(userId, invoiceId);
        invoice.TotalAmount = transactions.Sum(t => t.InstallmentAmount);
        invoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<InvoiceStatusSummary> PromotePendingAndMarkOverdueAsync()
    {
        var today = DateTime.UtcNow.Date;
        var promoted = 0;
        var closed = 0;
        var overdue = 0;

        var pendingInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Pending);
        foreach (var invoice in pendingInvoices)
        {
            var (refYear, refMonth) = CreditCardDateUtils.ParseReferenceMonth(invoice.ReferenceMonth);
            var firstDayOfReference = new DateTime(refYear, refMonth, 1);
            if (firstDayOfReference.Year < today.Year ||
                (firstDayOfReference.Year == today.Year && firstDayOfReference.Month <= today.Month))
            {
                invoice.Status = InvoiceStatus.Open;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
                promoted++;
            }
        }

        var justClosed = new List<CreditCardInvoice>();
        var openInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Open);
        foreach (var invoice in openInvoices)
        {
            if (invoice.ClosingDate.Date <= today)
            {
                invoice.Status = InvoiceStatus.Closed;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
                justClosed.Add(invoice);
                closed++;
            }
        }

        // Garantir fatura corrente após fechamento: promover pendente ou criar nova
        foreach (var closedInvoice in justClosed)
        {
            var nextRefMonth = CreditCardDateUtils.AddMonths(closedInvoice.ReferenceMonth, 1);
            var existingNext = await _unitOfWork.CreditCardInvoices.GetByCardAndReferenceAsync(
                closedInvoice.UserId, closedInvoice.CreditCardId, nextRefMonth);

            if (existingNext != null && existingNext.Status == InvoiceStatus.Pending)
            {
                existingNext.Status = InvoiceStatus.Open;
                existingNext.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(existingNext);
                promoted++;
            }
            else if (existingNext == null)
            {
                var card = await _unitOfWork.CreditCards.GetByIdAsync(closedInvoice.CreditCardId);
                if (card != null && !card.IsDeleted)
                    await GetOrCreateInvoiceAsync(closedInvoice.UserId, card, nextRefMonth, InvoiceStatus.Open);
            }
        }

        var closedInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Closed);
        foreach (var invoice in closedInvoices)
        {
            if (invoice.DueDate.Date < today)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
                overdue++;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        _processLogger.AddStep("Invoice status job complete", new Dictionary<string, object?>
        {
            ["promoted"] = promoted,
            ["closed"] = closed,
            ["overdue"] = overdue
        });

        return new InvoiceStatusSummary(promoted, closed, overdue);
    }

    private static CreditCardInvoiceResponseDto MapToDto(CreditCardInvoice invoice, CreditCard card)
    {
        return new CreditCardInvoiceResponseDto
        {
            Id = invoice.Id,
            CreditCardId = invoice.CreditCardId,
            CreditCardName = card.Name,
            ReferenceMonth = invoice.ReferenceMonth,
            ClosingDate = invoice.ClosingDate,
            DueDate = invoice.DueDate,
            Status = invoice.Status.ToString().ToLowerInvariant(),
            TotalAmount = invoice.TotalAmount,
            PaidAt = invoice.PaidAt,
            PaidWithAccountId = invoice.PaidWithAccountId,
            PaidAmount = invoice.PaidAmount,
            Currency = card.Currency,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }

    private static CreditCardTransactionResponseDto MapTransactionToDto(CreditCardTransaction transaction, CreditCard card, Category? category)
    {
        return new CreditCardTransactionResponseDto
        {
            Id = transaction.Id,
            CreditCardId = transaction.CreditCardId,
            InvoiceId = transaction.InvoiceId,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            CategoryColor = category?.Color ?? "#64748b",
            PurchaseDate = transaction.PurchaseDate,
            TotalAmount = transaction.TotalAmount,
            InstallmentAmount = transaction.InstallmentAmount,
            InstallmentNumber = transaction.InstallmentNumber,
            TotalInstallments = transaction.TotalInstallments,
            ParentTransactionId = transaction.ParentTransactionId,
            Currency = card.Currency,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
