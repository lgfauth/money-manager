using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardInvoiceServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ICreditCardRepository _cardRepo;
    private readonly ICreditCardInvoiceRepository _invoiceRepo;
    private readonly ICreditCardTransactionRepository _ccTransactionRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly ITransactionService _transactionServiceMock;
    private readonly CreditCardInvoiceService _service;

    private const string UserId = "user1";

    public CreditCardInvoiceServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _cardRepo = Substitute.For<ICreditCardRepository>();
        _invoiceRepo = Substitute.For<ICreditCardInvoiceRepository>();
        _ccTransactionRepo = Substitute.For<ICreditCardTransactionRepository>();
        _accountRepo = Substitute.For<IRepository<Account>>();
        _categoryRepo = Substitute.For<IRepository<Category>>();
        _transactionServiceMock = Substitute.For<ITransactionService>();

        _unitOfWorkMock.CreditCards.Returns(_cardRepo);
        _unitOfWorkMock.CreditCardInvoices.Returns(_invoiceRepo);
        _unitOfWorkMock.CreditCardTransactions.Returns(_ccTransactionRepo);
        _unitOfWorkMock.Accounts.Returns(_accountRepo);
        _unitOfWorkMock.Categories.Returns(_categoryRepo);

        _service = new CreditCardInvoiceService(
            _unitOfWorkMock,
            _transactionServiceMock,
            Substitute.For<IProcessLogger>());
    }

    private static CreditCard MakeCard(string id = "card1") => new()
    {
        Id = id,
        UserId = UserId,
        Name = "Cartão Teste",
        ClosingDay = 15,
        BillingDueDay = 22
    };

    [Fact]
    public async Task GetByCardAsync_WithUnknownCard_ShouldThrowKeyNotFound()
    {
        _cardRepo.GetByIdAsync("card1").Returns((CreditCard?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByCardAsync(UserId, "card1"));
    }

    [Fact]
    public async Task GetByCardAsync_WithCardOfAnotherUser_ShouldThrowKeyNotFound()
    {
        var card = MakeCard();
        card.UserId = "other";
        _cardRepo.GetByIdAsync("card1").Returns(card);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByCardAsync(UserId, "card1"));
    }

    [Fact]
    public async Task GetByCardAsync_ShouldReturnInvoicesOrderedByReferenceMonthDescending()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync("card1").Returns(card);

        var older = new CreditCardInvoice { UserId = UserId, CreditCardId = "card1", ReferenceMonth = "2026-05", Status = InvoiceStatus.Paid };
        var open = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(1)),
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(10),
            DueDate = DateTime.UtcNow.AddDays(17)
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { older, open });

        var result = (await _service.GetByCardAsync(UserId, "card1")).ToList();

        Assert.Equal(2, result.Count);
        Assert.True(string.Compare(result[0].ReferenceMonth, result[1].ReferenceMonth, StringComparison.Ordinal) > 0);
        Assert.All(result, dto => Assert.Equal("Cartão Teste", dto.CreditCardName));
    }

    [Fact]
    public async Task GetDetailAsync_ShouldReturnInvoiceWithTransactionsAndCategoryData()
    {
        var card = MakeCard();
        var invoice = new CreditCardInvoice { Id = "inv1", UserId = UserId, CreditCardId = "card1", Status = InvoiceStatus.Open };
        _invoiceRepo.GetByIdAsync("inv1").Returns(invoice);
        _cardRepo.GetByIdAsync("card1").Returns(card);

        var category = new Category { Id = "cat1", UserId = UserId, Name = "Mercado", Color = "#ff0000" };
        _categoryRepo.GetAllAsync().Returns(new List<Category> { category });

        _ccTransactionRepo.GetByInvoiceAsync(UserId, "inv1").Returns(new List<CreditCardTransaction>
        {
            new() { UserId = UserId, CreditCardId = "card1", InvoiceId = "inv1", CategoryId = "cat1", Description = "Compra A", PurchaseDate = DateTime.UtcNow.AddDays(-2) },
            new() { UserId = UserId, CreditCardId = "card1", InvoiceId = "inv1", CategoryId = null, Description = "Compra B", PurchaseDate = DateTime.UtcNow.AddDays(-5) }
        });

        var result = await _service.GetDetailAsync(UserId, "inv1");

        Assert.Equal(2, result.Transactions.Count);
        // Ordenado por data de compra ascendente
        Assert.Equal("Compra B", result.Transactions[0].Description);
        var withCategory = result.Transactions.Single(t => t.CategoryId == "cat1");
        Assert.Equal("Mercado", withCategory.CategoryName);
        Assert.Equal("#ff0000", withCategory.CategoryColor);
    }

    [Fact]
    public async Task GetDetailAsync_WithUnknownInvoice_ShouldThrowKeyNotFound()
    {
        _invoiceRepo.GetByIdAsync("inv1").Returns((CreditCardInvoice?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetDetailAsync(UserId, "inv1"));
    }

    [Fact]
    public async Task PayAsync_WithClosedInvoice_ShouldCreateDebitAndMarkPaid()
    {
        var card = MakeCard();
        var invoice = new CreditCardInvoice
        {
            Id = "inv1",
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = "2026-06",
            Status = InvoiceStatus.Closed,
            TotalAmount = 500m
        };
        _invoiceRepo.GetByIdAsync("inv1").Returns(invoice);
        _cardRepo.GetByIdAsync("card1").Returns(card);
        _accountRepo.GetByIdAsync("acc1").Returns(new Account { Id = "acc1", UserId = UserId });
        _transactionServiceMock.CreateAsync(UserId, Arg.Any<CreateTransactionRequestDto>())
            .Returns(new TransactionResponseDto { Id = "tx1" });

        var paidAt = DateTime.UtcNow;
        var result = await _service.PayAsync(UserId, "inv1", new PayCreditCardInvoiceRequestDto
        {
            PaidWithAccountId = "acc1",
            PaidAmount = 500m,
            PaidAt = paidAt
        });

        Assert.Equal("paid", result.Status);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal("tx1", invoice.PaymentTransactionId);
        Assert.Equal(500m, invoice.PaidAmount);
        Assert.Equal("acc1", invoice.PaidWithAccountId);
        await _transactionServiceMock.Received(1).CreateAsync(UserId, Arg.Is<CreateTransactionRequestDto>(r =>
            r.Type == TransactionType.Expense &&
            r.Amount == 500m &&
            r.AccountId == "acc1"));
        await _invoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Fact]
    public async Task PayAsync_WithOpenInvoice_ShouldThrowInvalidOperation()
    {
        var invoice = new CreditCardInvoice { Id = "inv1", UserId = UserId, Status = InvoiceStatus.Open };
        _invoiceRepo.GetByIdAsync("inv1").Returns(invoice);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.PayAsync(UserId, "inv1", new PayCreditCardInvoiceRequestDto { PaidWithAccountId = "acc1" }));
    }

    [Fact]
    public async Task PayAsync_WithUnknownAccount_ShouldThrowKeyNotFound()
    {
        var invoice = new CreditCardInvoice { Id = "inv1", UserId = UserId, Status = InvoiceStatus.Overdue, CreditCardId = "card1" };
        _invoiceRepo.GetByIdAsync("inv1").Returns(invoice);
        _accountRepo.GetByIdAsync("acc1").Returns((Account?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.PayAsync(UserId, "inv1", new PayCreditCardInvoiceRequestDto { PaidWithAccountId = "acc1" }));
    }

    [Fact]
    public async Task GetOrCreateInvoiceAsync_WithExistingInvoice_ShouldReturnItWithoutCreating()
    {
        var card = MakeCard();
        var existing = new CreditCardInvoice { UserId = UserId, CreditCardId = "card1", ReferenceMonth = "2026-07" };
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", "2026-07").Returns(existing);

        var result = await _service.GetOrCreateInvoiceAsync(UserId, card, "2026-07", InvoiceStatus.Open);

        Assert.Same(existing, result);
        await _invoiceRepo.DidNotReceive().AddAsync(Arg.Any<CreditCardInvoice>());
    }

    [Fact]
    public async Task GetOrCreateInvoiceAsync_WithoutExisting_ShouldCreateWithComputedDates()
    {
        var card = MakeCard(); // fecha dia 15, vence dia 22
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", "2026-07").Returns((CreditCardInvoice?)null);
        _invoiceRepo.AddAsync(Arg.Any<CreditCardInvoice>()).Returns(x => x.Arg<CreditCardInvoice>());

        var result = await _service.GetOrCreateInvoiceAsync(UserId, card, "2026-07", InvoiceStatus.Pending);

        Assert.Equal("2026-07", result.ReferenceMonth);
        Assert.Equal(InvoiceStatus.Pending, result.Status);
        Assert.Equal(new DateTime(2026, 7, 15), result.ClosingDate.Date);
        Assert.Equal(new DateTime(2026, 7, 22), result.DueDate.Date);
        Assert.Equal(0, result.TotalAmount);
        await _invoiceRepo.Received(1).AddAsync(Arg.Any<CreditCardInvoice>());
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task EnsureCurrentOpenInvoiceAsync_WithoutAnyInvoice_ShouldCreateOpenInvoiceForCurrentCycle()
    {
        var card = MakeCard();
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice>());
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", Arg.Any<string>()).Returns((CreditCardInvoice?)null);
        _invoiceRepo.AddAsync(Arg.Any<CreditCardInvoice>()).Returns(x => x.Arg<CreditCardInvoice>());

        var result = await _service.EnsureCurrentOpenInvoiceAsync(UserId, card);

        var expectedReference = CreditCardDateUtils.ReferenceMonthForPurchaseDate(DateTime.UtcNow.Date, card.ClosingDay);
        Assert.Equal(expectedReference, result.ReferenceMonth);
        Assert.Equal(InvoiceStatus.Open, result.Status);
    }

    [Fact]
    public async Task EnsureCurrentOpenInvoiceAsync_WithOpenInvoiceStillInCycle_ShouldReturnIt()
    {
        var card = MakeCard();
        var open = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(1)),
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(10),
            DueDate = DateTime.UtcNow.AddDays(17)
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { open });

        var result = await _service.EnsureCurrentOpenInvoiceAsync(UserId, card);

        Assert.Same(open, result);
        await _invoiceRepo.DidNotReceive().AddAsync(Arg.Any<CreditCardInvoice>());
    }

    [Fact]
    public async Task EnsureCurrentOpenInvoiceAsync_WithOpenInvoicePastClosingDate_ShouldCloseIt()
    {
        var card = MakeCard();
        var stale = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(-1)),
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(-3)
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { stale });
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", Arg.Any<string>()).Returns((CreditCardInvoice?)null);
        _invoiceRepo.AddAsync(Arg.Any<CreditCardInvoice>()).Returns(x => x.Arg<CreditCardInvoice>());

        var result = await _service.EnsureCurrentOpenInvoiceAsync(UserId, card);

        // Fatura antiga fecha (e vence) e uma nova é aberta para o ciclo corrente
        Assert.Equal(InvoiceStatus.Overdue, stale.Status);
        Assert.Equal(InvoiceStatus.Open, result.Status);
        Assert.NotSame(stale, result);
    }

    [Fact]
    public async Task RecalculateTotalAsync_ShouldSumInstallmentAmounts()
    {
        var invoice = new CreditCardInvoice { Id = "inv1", UserId = UserId, TotalAmount = 0 };
        _invoiceRepo.GetByIdAsync("inv1").Returns(invoice);
        _ccTransactionRepo.GetByInvoiceAsync(UserId, "inv1").Returns(new List<CreditCardTransaction>
        {
            new() { InstallmentAmount = 100.50m },
            new() { InstallmentAmount = 49.50m },
            new() { InstallmentAmount = -20m } // estorno
        });

        await _service.RecalculateTotalAsync(UserId, "inv1");

        Assert.Equal(130m, invoice.TotalAmount);
        await _invoiceRepo.Received(1).UpdateAsync(invoice);
    }

    [Fact]
    public async Task RecalculateTotalAsync_WithUnknownInvoice_ShouldDoNothing()
    {
        _invoiceRepo.GetByIdAsync("inv1").Returns((CreditCardInvoice?)null);

        await _service.RecalculateTotalAsync(UserId, "inv1");

        await _invoiceRepo.DidNotReceive().UpdateAsync(Arg.Any<CreditCardInvoice>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateOrCreateOpenInvoiceAsync_WithOpenInvoice_ShouldApplySyncValues()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync("card1").Returns(card);
        var newDueDate = DateTime.UtcNow.AddDays(20);
        var newCloseDate = DateTime.UtcNow.AddDays(13);
        var referenceMonth = CreditCardDateUtils.FormatReferenceMonth(newCloseDate);

        var open = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = referenceMonth,
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(10),
            DueDate = DateTime.UtcNow.AddDays(17)
        };
        // A fatura agora é resolvida pelo mês de referência implicado pelo CloseDate do banco
        // (não mais "a fatura aberta mais antiga"), então o mock precisa responder por esse lookup.
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", referenceMonth).Returns(open);

        var result = await _service.UpdateOrCreateOpenInvoiceAsync(UserId, "card1", new UpdateOpenInvoiceFromSyncDto
        {
            TotalAmount = 987.65m,
            DueDate = newDueDate,
            CloseDate = newCloseDate
        }, CancellationToken.None);

        Assert.Equal(987.65m, open.TotalAmount);
        Assert.Equal(newDueDate, open.DueDate);
        Assert.Equal(newCloseDate, open.ClosingDate);
        Assert.Equal("open", result.Status);
        await _invoiceRepo.Received(1).UpdateAsync(open);
    }

    [Fact]
    public async Task UpdateOrCreateOpenInvoiceAsync_WithUnknownCard_ShouldThrowKeyNotFound()
    {
        _cardRepo.GetByIdAsync("card1").Returns((CreditCard?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateOrCreateOpenInvoiceAsync(
            UserId, "card1", new UpdateOpenInvoiceFromSyncDto(), CancellationToken.None));
    }

    [Fact]
    public async Task OpenCurrentInvoiceAsync_WithExistingOpenInvoice_ShouldReturnIt()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync("card1").Returns(card);
        var open = new CreditCardInvoice
        {
            Id = "inv1",
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = "2026-07",
            Status = InvoiceStatus.Open
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { open });

        var result = await _service.OpenCurrentInvoiceAsync(UserId, "card1");

        Assert.Equal("inv1", result.Id);
        await _invoiceRepo.DidNotReceive().AddAsync(Arg.Any<CreditCardInvoice>());
    }

    [Fact]
    public async Task OpenCurrentInvoiceAsync_WithPendingInvoiceForCurrentCycle_ShouldPromoteToOpen()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync("card1").Returns(card);
        var currentReference = CreditCardDateUtils.ReferenceMonthForPurchaseDate(DateTime.UtcNow.Date, card.ClosingDay);
        var pending = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = currentReference,
            Status = InvoiceStatus.Pending
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { pending });

        var result = await _service.OpenCurrentInvoiceAsync(UserId, "card1");

        Assert.Equal(InvoiceStatus.Open, pending.Status);
        Assert.Equal("open", result.Status);
    }

    [Fact]
    public async Task OpenCurrentInvoiceAsync_WithTerminalInvoiceInCurrentCycle_ShouldCreateForNextPeriod()
    {
        var card = MakeCard();
        _cardRepo.GetByIdAsync("card1").Returns(card);
        var currentReference = CreditCardDateUtils.ReferenceMonthForPurchaseDate(DateTime.UtcNow.Date, card.ClosingDay);
        var paid = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = currentReference,
            Status = InvoiceStatus.Paid
        };
        _invoiceRepo.GetByCardAsync(UserId, "card1").Returns(new List<CreditCardInvoice> { paid });
        _invoiceRepo.GetByCardAndReferenceAsync(UserId, "card1", Arg.Any<string>()).Returns((CreditCardInvoice?)null);
        _invoiceRepo.AddAsync(Arg.Any<CreditCardInvoice>()).Returns(x => x.Arg<CreditCardInvoice>());

        var result = await _service.OpenCurrentInvoiceAsync(UserId, "card1");

        var nextReference = CreditCardDateUtils.AddMonths(currentReference, 1);
        Assert.Equal(nextReference, result.ReferenceMonth);
        Assert.Equal("open", result.Status);
    }

    [Fact]
    public async Task PromotePendingAndMarkOverdueAsync_ShouldPromoteCloseAndMarkOverdue()
    {
        var reachedPending = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow),
            Status = InvoiceStatus.Pending
        };
        var futurePending = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(2)),
            Status = InvoiceStatus.Pending
        };
        var openPastClosing = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(-1)),
            Status = InvoiceStatus.Open,
            ClosingDate = DateTime.UtcNow.AddDays(-3),
            DueDate = DateTime.UtcNow.AddDays(4)
        };
        var closedPastDue = new CreditCardInvoice
        {
            UserId = UserId,
            CreditCardId = "card1",
            ReferenceMonth = CreditCardDateUtils.FormatReferenceMonth(DateTime.UtcNow.AddMonths(-2)),
            Status = InvoiceStatus.Closed,
            ClosingDate = DateTime.UtcNow.AddDays(-40),
            DueDate = DateTime.UtcNow.AddDays(-30)
        };

        _invoiceRepo.GetByStatusAsync(InvoiceStatus.Pending)
            .Returns(new List<CreditCardInvoice> { reachedPending, futurePending });
        _invoiceRepo.GetByStatusAsync(InvoiceStatus.Open)
            .Returns(new List<CreditCardInvoice> { openPastClosing });
        _invoiceRepo.GetByStatusAsync(InvoiceStatus.Closed)
            .Returns(new List<CreditCardInvoice> { closedPastDue });
        _invoiceRepo.GetByStatusAsync(InvoiceStatus.Overdue)
            .Returns(new List<CreditCardInvoice>());
        // Faturas seguintes já existem em estado Open — não requerem intervenção
        _invoiceRepo.GetByCardAndReferenceAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new CreditCardInvoice { Status = InvoiceStatus.Open });

        var summary = await _service.PromotePendingAndMarkOverdueAsync();

        Assert.Equal(1, summary.PromotedToOpen);
        Assert.Equal(1, summary.ClosedInvoices);
        Assert.Equal(1, summary.MarkedOverdue);
        Assert.Equal(InvoiceStatus.Open, reachedPending.Status);
        Assert.Equal(InvoiceStatus.Pending, futurePending.Status);
        Assert.Equal(InvoiceStatus.Closed, openPastClosing.Status);
        Assert.Equal(InvoiceStatus.Overdue, closedPastDue.Status);
        await _unitOfWorkMock.Received(1).SaveChangesAsync();
    }
}
