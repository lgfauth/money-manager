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
    Task<CreditCardInvoiceResponseDto> UpdateOrCreateOpenInvoiceAsync(
        string userId,
        string creditCardId,
        UpdateOpenInvoiceFromSyncDto request,
        CancellationToken ct);

    Task<CreditCardInvoice> EnsureCurrentOpenInvoiceAsync(string userId, CreditCard card);
    Task<CreditCardInvoice> GetOrCreateInvoiceAsync(string userId, CreditCard card, string referenceMonth, InvoiceStatus initialStatus);
    Task RecalculateTotalAsync(string userId, string invoiceId);
    Task<InvoiceStatusSummary> PromotePendingAndMarkOverdueAsync();
    Task<int> SyncPaymentStatusFromBankAsync(
        string userId,
        string creditCardId,
        IReadOnlyList<BankMcpCreditCardBill> remoteBills,
        CancellationToken ct);
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
        var invoices = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, creditCardId)).ToList();

        // Se já existe uma fatura aberta em qualquer período, não há nada a fazer
        var anyOpen = invoices.FirstOrDefault(i => i.Status == InvoiceStatus.Open);
        if (anyOpen != null)
            return MapToDto(anyOpen, card);

        // Determinar o período inicial de busca: o referenceMonth calculado a partir de hoje
        var referenceMonth = CreditCardDateUtils.ReferenceMonthForPurchaseDate(today, card.ClosingDay);

        // Avançar período a período até encontrar um que possa ser aberto:
        // - se existe como Pending → promover para Open
        // - se não existe → criar como Open
        // - se existe com status terminal (Closed, Paid, Overdue) → avançar para o próximo mês
        const int maxLookAhead = 3;
        for (var i = 0; i < maxLookAhead; i++)
        {
            var existing = invoices.FirstOrDefault(inv =>
                inv.ReferenceMonth == referenceMonth && !inv.IsDeleted);

            if (existing == null)
            {
                var newInvoice = await GetOrCreateInvoiceAsync(userId, card, referenceMonth, InvoiceStatus.Open);
                return MapToDto(newInvoice, card);
            }

            if (existing.Status == InvoiceStatus.Pending)
            {
                existing.Status = InvoiceStatus.Open;
                existing.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(existing);
                await _unitOfWork.SaveChangesAsync();
                return MapToDto(existing, card);
            }

            // Status terminal (Closed, Paid, Overdue): avançar para o próximo período
            referenceMonth = CreditCardDateUtils.AddMonths(referenceMonth, 1);
        }

        throw new InvalidOperationException("Não foi possível encontrar um período disponível para abertura de fatura.");
    }

    public async Task<CreditCardInvoiceResponseDto> UpdateOrCreateOpenInvoiceAsync(
        string userId,
        string creditCardId,
        UpdateOpenInvoiceFromSyncDto request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var card = await _unitOfWork.CreditCards.GetByIdAsync(creditCardId);
        if (card == null || card.UserId != userId || card.IsDeleted)
            throw new KeyNotFoundException("Credit card not found");

        // Resolve a fatura pelo mês de referência implicado pela data de fechamento reportada
        // pelo banco, em vez de assumir "a fatura aberta mais antiga": se o ClosingDay do cartão
        // tiver sido corrigido nesta mesma sincronização (ver SyncConnectionAsync), a fatura
        // "aberta" mais antiga pode não ser mais a fatura do período corrente, o que faria este
        // método atualizar o total de uma fatura diferente daquela que recebe as transações.
        var openInvoice = request.CloseDate.HasValue
            ? await GetOrCreateInvoiceAsync(
                userId,
                card,
                CreditCardDateUtils.FormatReferenceMonth(request.CloseDate.Value),
                InvoiceStatus.Open)
            : (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, creditCardId))
                .Where(i => i.Status == InvoiceStatus.Open)
                .OrderBy(i => i.ReferenceMonth)
                .FirstOrDefault()
              ?? await EnsureCurrentOpenInvoiceAsync(userId, card);

        openInvoice.TotalAmount = request.TotalAmount;
        openInvoice.DueDate = request.DueDate;
        if (request.CloseDate.HasValue)
            openInvoice.ClosingDate = request.CloseDate.Value;

        openInvoice.Status = InvoiceStatus.Open;
        openInvoice.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.CreditCardInvoices.UpdateAsync(openInvoice);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(openInvoice, card);
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

        var openInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Open);
        foreach (var invoice in openInvoices)
        {
            if (invoice.ClosingDate.Date <= today)
            {
                invoice.Status = InvoiceStatus.Closed;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
                closed++;
            }
        }

        // Garantir abertura da fatura seguinte após fechamento.
        // Processa todas as faturas Closed e Overdue para cobrir casos em que a abertura
        // falhou em execuções anteriores (falha parcial, restart do worker, timeout, etc.).
        var closedInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Closed);
        var overdueInvoices = await _unitOfWork.CreditCardInvoices.GetByStatusAsync(InvoiceStatus.Overdue);

        foreach (var closedInvoice in closedInvoices.Concat(overdueInvoices))
        {
            var nextRefMonth = CreditCardDateUtils.AddMonths(closedInvoice.ReferenceMonth, 1);
            var existingNext = await _unitOfWork.CreditCardInvoices.GetByCardAndReferenceAsync(
                closedInvoice.UserId, closedInvoice.CreditCardId, nextRefMonth);

            // Fatura seguinte já existe e está em estado que não requer intervenção
            if (existingNext != null && existingNext.Status != InvoiceStatus.Pending)
                continue;

            var card = await _unitOfWork.CreditCards.GetByIdAsync(closedInvoice.CreditCardId);
            if (card == null || card.IsDeleted) continue;

            // Processar somente se o próximo período não ultrapassa o ciclo de cobrança atual.
            // Usa today + 1 dia para tratar corretamente o caso em que hoje é o dia de fechamento,
            // onde ReferenceMonthForPurchaseDate(today) ainda retorna o mês corrente (que está fechando).
            var currentRefMonth = CreditCardDateUtils.ReferenceMonthForPurchaseDate(today.AddDays(1), card.ClosingDay);
            if (string.Compare(nextRefMonth, currentRefMonth, StringComparison.Ordinal) > 0) continue;

            if (existingNext != null)
            {
                existingNext.Status = InvoiceStatus.Open;
                existingNext.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CreditCardInvoices.UpdateAsync(existingNext);
                promoted++;
            }
            else
            {
                await GetOrCreateInvoiceAsync(closedInvoice.UserId, card, nextRefMonth, InvoiceStatus.Open);
            }
        }

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

    public async Task<int> SyncPaymentStatusFromBankAsync(
        string userId,
        string creditCardId,
        IReadOnlyList<BankMcpCreditCardBill> remoteBills,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // O Banco MCP deriva "payment_status" cruzando faturas (Open Finance BR não expõe um
        // campo "paga" nativo). Só "PAID" é uma confirmação confiável de pagamento — os demais
        // status (OPEN, PAST_DUE_UNCONFIRMED, PAST_DUE_UNPAID) não devem alterar o status local,
        // que já é mantido por data em EnsureCurrentOpenInvoiceAsync/PromotePendingAndMarkOverdueAsync.
        var paidRemoteBills = remoteBills
            .Where(b => b.BillClosingDate.HasValue
                && string.Equals(b.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (paidRemoteBills.Count == 0)
            return 0;

        var localInvoicesByReference = (await _unitOfWork.CreditCardInvoices.GetByCardAsync(userId, creditCardId))
            .Where(i => i.Status == InvoiceStatus.Closed || i.Status == InvoiceStatus.Overdue)
            .ToDictionary(i => i.ReferenceMonth);

        if (localInvoicesByReference.Count == 0)
            return 0;

        var markedPaid = 0;

        foreach (var remoteBill in paidRemoteBills)
        {
            var referenceMonth = CreditCardDateUtils.FormatReferenceMonth(remoteBill.BillClosingDate!.Value);
            if (!localInvoicesByReference.TryGetValue(referenceMonth, out var invoice))
                continue;

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = FindPaymentDate(remoteBill, remoteBills);
            invoice.PaidAmount = remoteBill.TotalAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
            markedPaid++;
        }

        if (markedPaid > 0)
        {
            await _unitOfWork.SaveChangesAsync();
            _processLogger.AddStep("Faturas marcadas como pagas via sincronização bancária", new Dictionary<string, object?>
            {
                ["creditCardId"] = creditCardId,
                ["markedPaid"] = markedPaid
            });
        }

        return markedPaid;
    }

    // Melhor esforço para exibir uma data de pagamento: procura, entre os pagamentos da própria
    // fatura (pré-pagamento antes do fechamento) e das faturas mais novas (pagamento feito entre
    // fechamento e vencimento, ou após), um valor aproximado do total da fatura. Se nada bater,
    // o status ainda é marcado como Paid (confiando no payment_status do Banco MCP) mas sem data.
    private static DateTime? FindPaymentDate(BankMcpCreditCardBill bill, IReadOnlyList<BankMcpCreditCardBill> allBills)
    {
        const decimal tolerance = 0.5m;
        bool Matches(BankMcpCreditCardBillPayment payment) => Math.Abs(payment.Amount - bill.TotalAmount) <= tolerance;

        var ownPayment = bill.Payments.FirstOrDefault(Matches);
        if (ownPayment is not null)
            return ownPayment.PaymentDate;

        return allBills
            .Where(b => b.BillClosingDate.HasValue && b.BillClosingDate > bill.BillClosingDate)
            .SelectMany(b => b.Payments)
            .Where(Matches)
            .OrderBy(p => p.PaymentDate)
            .Select(p => (DateTime?)p.PaymentDate)
            .FirstOrDefault();
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
