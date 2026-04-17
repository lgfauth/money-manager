using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface ICreditCardTransactionService
{
    Task<IEnumerable<CreditCardTransactionResponseDto>> CreateAsync(string userId, CreateCreditCardTransactionRequestDto request);
    Task<IEnumerable<CreditCardTransactionResponseDto>> GetAllAsync(string userId);
    Task<IEnumerable<CreditCardTransactionResponseDto>> GetByCardAsync(string userId, string creditCardId);
    Task DeleteAsync(string userId, string id);
}

public class CreditCardTransactionService : ICreditCardTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly IProcessLogger _processLogger;

    public CreditCardTransactionService(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _processLogger = processLogger;
    }

    public async Task<IEnumerable<CreditCardTransactionResponseDto>> CreateAsync(string userId, CreateCreditCardTransactionRequestDto request)
    {
        _processLogger.AddStep("Creating credit card transaction", new Dictionary<string, object?>
        {
            ["creditCardId"] = request.CreditCardId,
            ["totalAmount"] = request.TotalAmount,
            ["installments"] = request.TotalInstallments
        });

        if (!string.IsNullOrEmpty(request.ClientRequestId))
        {
            var allForUser = await _unitOfWork.CreditCardTransactions.GetByUserAsync(userId);
            var duplicateParent = allForUser.FirstOrDefault(t =>
                t.ParentTransactionId == null &&
                t.Description == request.Description &&
                t.TotalAmount == request.TotalAmount &&
                t.PurchaseDate.Date == request.PurchaseDate.Date &&
                t.CreditCardId == request.CreditCardId);
            if (duplicateParent != null)
            {
                _processLogger.AddWarning("Possible duplicate credit card transaction detected", new Dictionary<string, object?>
                {
                    ["clientRequestId"] = request.ClientRequestId
                });
            }
        }

        var card = await _unitOfWork.CreditCards.GetByIdAsync(request.CreditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        await _invoiceService.EnsureCurrentOpenInvoiceAsync(userId, card);

        var totalInstallments = Math.Max(1, request.TotalInstallments);
        var installmentAmount = Math.Round(request.TotalAmount / totalInstallments, 2, MidpointRounding.AwayFromZero);

        var roundingDiff = request.TotalAmount - (installmentAmount * totalInstallments);

        string? parentId = null;
        var created = new List<CreditCardTransaction>();
        var invoiceIdsTouched = new HashSet<string>();

        for (var i = 1; i <= totalInstallments; i++)
        {
            var monthOffset = request.FirstInstallmentOnCurrentInvoice ? i - 1 : i;
            var targetDate = CreditCardDateUtils.SafeInstallmentDate(request.PurchaseDate, monthOffset);
            var referenceMonth = CreditCardDateUtils.ReferenceMonthForPurchaseDate(targetDate, card.ClosingDay);

            var initialStatus = ResolveInitialStatusForInstallment(referenceMonth);
            var invoice = await _invoiceService.GetOrCreateInvoiceAsync(userId, card, referenceMonth, initialStatus);

            if (invoice.Status == InvoiceStatus.Closed || invoice.Status == InvoiceStatus.Paid || invoice.Status == InvoiceStatus.Overdue)
            {
                throw new InvalidOperationException($"Invoice for {referenceMonth} is not accepting new transactions");
            }

            var amountForInstallment = installmentAmount;
            if (i == totalInstallments && roundingDiff != 0)
            {
                amountForInstallment += roundingDiff;
            }

            var transaction = new CreditCardTransaction
            {
                UserId = userId,
                CreditCardId = card.Id,
                InvoiceId = invoice.Id,
                Description = request.Description,
                CategoryId = request.CategoryId,
                PurchaseDate = request.PurchaseDate,
                TotalAmount = request.TotalAmount,
                InstallmentAmount = amountForInstallment,
                InstallmentNumber = i,
                TotalInstallments = totalInstallments,
                ParentTransactionId = parentId
            };

            await _unitOfWork.CreditCardTransactions.AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            if (i == 1)
            {
                parentId = transaction.Id;
            }

            invoiceIdsTouched.Add(invoice.Id);
            created.Add(transaction);
        }

        foreach (var invoiceId in invoiceIdsTouched)
        {
            await _invoiceService.RecalculateTotalAsync(userId, invoiceId);
        }

        _processLogger.AddStep("Credit card transaction created", new Dictionary<string, object?>
        {
            ["parentId"] = parentId,
            ["installmentsCreated"] = created.Count
        });

        var categories = (await _unitOfWork.Categories.GetAllAsync())
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .ToDictionary(c => c.Id);

        return created.Select(t =>
        {
            Category? category = null;
            if (!string.IsNullOrWhiteSpace(t.CategoryId))
            {
                categories.TryGetValue(t.CategoryId, out category);
            }
            return MapToDto(t, card, category);
        });
    }

    public async Task<IEnumerable<CreditCardTransactionResponseDto>> GetAllAsync(string userId)
    {
        var transactions = (await _unitOfWork.CreditCardTransactions.GetByUserAsync(userId)).ToList();
        return await BuildDtosAsync(userId, transactions);
    }

    public async Task<IEnumerable<CreditCardTransactionResponseDto>> GetByCardAsync(string userId, string creditCardId)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(creditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        var transactions = (await _unitOfWork.CreditCardTransactions.GetByCardAsync(userId, creditCardId)).ToList();
        return await BuildDtosAsync(userId, transactions);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        _processLogger.AddStep("Deleting credit card transaction", new Dictionary<string, object?> { ["transactionId"] = id });

        var transaction = await _unitOfWork.CreditCardTransactions.GetByIdAsync(id);
        if (transaction == null || transaction.UserId != userId || transaction.IsDeleted)
            throw new KeyNotFoundException("Transaction not found");

        var invoice = await _unitOfWork.CreditCardInvoices.GetByIdAsync(transaction.InvoiceId);
        if (invoice == null || invoice.IsDeleted)
            throw new KeyNotFoundException("Invoice not found");

        var parentId = transaction.ParentTransactionId ?? transaction.Id;
        var siblings = (await _unitOfWork.CreditCardTransactions.GetByParentAsync(userId, parentId)).ToList();
        var allInstallments = new List<CreditCardTransaction> { transaction };

        if (transaction.ParentTransactionId == null)
        {
            allInstallments.AddRange(siblings.Where(s => s.Id != transaction.Id));
        }
        else
        {
            var parent = await _unitOfWork.CreditCardTransactions.GetByIdAsync(parentId);
            if (parent != null && !parent.IsDeleted)
            {
                allInstallments.Add(parent);
            }
            allInstallments.AddRange(siblings.Where(s => s.Id != transaction.Id));
        }

        var invoiceIdsTouched = new HashSet<string>();
        foreach (var installment in allInstallments.DistinctBy(t => t.Id))
        {
            var inv = installment.Id == transaction.Id
                ? invoice
                : await _unitOfWork.CreditCardInvoices.GetByIdAsync(installment.InvoiceId);

            if (inv == null || inv.IsDeleted)
                continue;

            if (inv.Status != InvoiceStatus.Open && inv.Status != InvoiceStatus.Pending)
                throw new InvalidOperationException("Cannot delete transactions linked to closed, paid or overdue invoices");

            installment.IsDeleted = true;
            installment.UpdatedAt = DateTime.UtcNow;
            installment.Version += 1;
            await _unitOfWork.CreditCardTransactions.UpdateAsync(installment);
            invoiceIdsTouched.Add(inv.Id);
        }

        await _unitOfWork.SaveChangesAsync();

        foreach (var invoiceId in invoiceIdsTouched)
        {
            await _invoiceService.RecalculateTotalAsync(userId, invoiceId);
        }

        _processLogger.AddStep("Credit card transaction deleted", new Dictionary<string, object?> { ["transactionId"] = id });
    }

    private static InvoiceStatus ResolveInitialStatusForInstallment(string referenceMonth)
    {
        var (refYear, refMonth) = CreditCardDateUtils.ParseReferenceMonth(referenceMonth);
        var today = DateTime.UtcNow.Date;
        var firstDay = new DateTime(refYear, refMonth, 1);

        if (firstDay.Year < today.Year ||
            (firstDay.Year == today.Year && firstDay.Month <= today.Month))
        {
            return InvoiceStatus.Open;
        }

        return InvoiceStatus.Pending;
    }

    private async Task<List<CreditCardTransactionResponseDto>> BuildDtosAsync(string userId, List<CreditCardTransaction> transactions)
    {
        if (transactions.Count == 0)
            return [];

        var cardIds = transactions.Select(t => t.CreditCardId).Distinct().ToHashSet();
        var cards = (await _unitOfWork.CreditCards.GetByUserAsync(userId))
            .Where(c => cardIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        var categoryIds = transactions
            .Where(t => !string.IsNullOrWhiteSpace(t.CategoryId))
            .Select(t => t.CategoryId!)
            .Distinct()
            .ToHashSet();

        var categories = (await _unitOfWork.Categories.GetAllAsync())
            .Where(c => c.UserId == userId && !c.IsDeleted && categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        return transactions.Select(t =>
        {
            cards.TryGetValue(t.CreditCardId, out var card);
            Category? category = null;
            if (!string.IsNullOrWhiteSpace(t.CategoryId))
            {
                categories.TryGetValue(t.CategoryId, out category);
            }
            return MapToDto(t, card, category);
        }).ToList();
    }

    private static CreditCardTransactionResponseDto MapToDto(CreditCardTransaction transaction, CreditCard? card, Category? category)
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
            Currency = card?.Currency ?? "BRL",
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
