using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Application.Services;

public interface ICreditCardService
{
    Task<CreditCardResponseDto> CreateAsync(string userId, CreateCreditCardRequestDto request);
    Task<IEnumerable<CreditCardResponseDto>> GetAllAsync(string userId);
    Task<CreditCardResponseDto> GetByIdAsync(string userId, string id);
    Task<CreditCardResponseDto> UpdateAsync(string userId, string id, CreateCreditCardRequestDto request);
    Task DeleteAsync(string userId, string id);
}

public class CreditCardService : ICreditCardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly IProcessLogger _processLogger;

    public CreditCardService(
        IUnitOfWork unitOfWork,
        ICreditCardInvoiceService invoiceService,
        IProcessLogger processLogger)
    {
        _unitOfWork = unitOfWork;
        _invoiceService = invoiceService;
        _processLogger = processLogger;
    }

    public async Task<CreditCardResponseDto> CreateAsync(string userId, CreateCreditCardRequestDto request)
    {
        _processLogger.AddStep("Creating credit card", new Dictionary<string, object?>
        {
            ["name"] = request.Name,
            ["limit"] = request.Limit
        });

        var card = new CreditCard
        {
            UserId = userId,
            Name = request.Name,
            Limit = request.Limit,
            ClosingDay = request.ClosingDay,
            BillingDueDay = request.BillingDueDay,
            BestPurchaseDay = request.BestPurchaseDay ?? request.ClosingDay,
            Color = request.Color,
            Currency = request.Currency
        };

        await _unitOfWork.CreditCards.AddAsync(card);
        await _unitOfWork.SaveChangesAsync();

        var firstInvoice = await _invoiceService.EnsureCurrentOpenInvoiceAsync(userId, card);

        _processLogger.AddStep("Credit card and first invoice created", new Dictionary<string, object?>
        {
            ["creditCardId"] = card.Id,
            ["invoiceId"] = firstInvoice.Id,
            ["referenceMonth"] = firstInvoice.ReferenceMonth
        });

        return await MapToDtoAsync(userId, card);
    }

    public async Task<IEnumerable<CreditCardResponseDto>> GetAllAsync(string userId)
    {
        var cards = (await _unitOfWork.CreditCards.GetByUserAsync(userId)).ToList();

        var result = new List<CreditCardResponseDto>();
        foreach (var card in cards)
        {
            await _invoiceService.EnsureCurrentOpenInvoiceAsync(userId, card);
            result.Add(await MapToDtoAsync(userId, card));
        }

        return result;
    }

    public async Task<CreditCardResponseDto> GetByIdAsync(string userId, string id)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        await _invoiceService.EnsureCurrentOpenInvoiceAsync(userId, card);

        return await MapToDtoAsync(userId, card);
    }

    public async Task<CreditCardResponseDto> UpdateAsync(string userId, string id, CreateCreditCardRequestDto request)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        card.Name = request.Name;
        card.Limit = request.Limit;
        card.ClosingDay = request.ClosingDay;
        card.BillingDueDay = request.BillingDueDay;
        card.BestPurchaseDay = request.BestPurchaseDay ?? request.ClosingDay;
        card.Color = request.Color;
        card.Currency = request.Currency;
        card.UpdatedAt = DateTime.UtcNow;
        card.Version += 1;

        await _unitOfWork.CreditCards.UpdateAsync(card);
        await _unitOfWork.SaveChangesAsync();

        return await MapToDtoAsync(userId, card);
    }

    public async Task DeleteAsync(string userId, string id)
    {
        var card = await _unitOfWork.CreditCards.GetByIdAsync(id);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        card.IsDeleted = true;
        card.UpdatedAt = DateTime.UtcNow;
        card.Version += 1;
        await _unitOfWork.CreditCards.UpdateAsync(card);

        var invoices = await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, id);
        foreach (var invoice in invoices)
        {
            invoice.IsDeleted = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.Version += 1;
            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
        }

        var transactions = await _unitOfWork.CreditCardTransactions.GetByCardAsync(userId, id);
        foreach (var transaction in transactions)
        {
            transaction.IsDeleted = true;
            transaction.UpdatedAt = DateTime.UtcNow;
            transaction.Version += 1;
            await _unitOfWork.CreditCardTransactions.UpdateAsync(transaction);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<CreditCardResponseDto> MapToDtoAsync(string userId, CreditCard card)
    {
        var invoices = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, card.Id)).ToList();

        var currentOpen = invoices
            .Where(i => i.Status == InvoiceStatus.Open)
            .OrderBy(i => i.ReferenceMonth)
            .FirstOrDefault();

        var outstanding = invoices
            .Where(i => i.Status == InvoiceStatus.Open ||
                        i.Status == InvoiceStatus.Pending ||
                        i.Status == InvoiceStatus.Closed ||
                        i.Status == InvoiceStatus.Overdue)
            .Sum(i => i.TotalAmount);

        return new CreditCardResponseDto
        {
            Id = card.Id,
            Name = card.Name,
            Limit = card.Limit,
            CurrentBalance = outstanding,
            AvailableLimit = card.Limit - outstanding,
            ClosingDay = card.ClosingDay,
            BillingDueDay = card.BillingDueDay,
            BestPurchaseDay = card.BestPurchaseDay,
            Color = card.Color,
            Currency = card.Currency,
            CurrentInvoice = currentOpen == null ? null : new CreditCardInvoiceSummaryDto
            {
                Id = currentOpen.Id,
                ReferenceMonth = currentOpen.ReferenceMonth,
                ClosingDate = currentOpen.ClosingDate,
                DueDate = currentOpen.DueDate,
                TotalAmount = currentOpen.TotalAmount,
                Status = currentOpen.Status.ToString().ToLowerInvariant()
            },
            CreatedAt = card.CreatedAt,
            UpdatedAt = card.UpdatedAt
        };
    }
}
