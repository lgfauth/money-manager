using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class CreditCardInvoiceServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IProcessLogger _processLoggerMock;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly ICreditCardInvoiceRepository _invoiceRepoMock;
    private readonly ITransactionRepository _transactionRepoMock;
    private readonly IRepository<Account> _accountRepoMock;
    private readonly IRepository<Category> _categoryRepoMock;

    public CreditCardInvoiceServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _processLoggerMock = Substitute.For<IProcessLogger>();
        _invoiceRepoMock = Substitute.For<ICreditCardInvoiceRepository>();
        _transactionRepoMock = Substitute.For<ITransactionRepository>();
        _accountRepoMock = Substitute.For<IRepository<Account>>();
        _categoryRepoMock = Substitute.For<IRepository<Category>>();
        
        _unitOfWorkMock.CreditCardInvoices.Returns(_invoiceRepoMock);
        _unitOfWorkMock.Transactions.Returns(_transactionRepoMock);
        _unitOfWorkMock.Accounts.Returns(_accountRepoMock);
        _unitOfWorkMock.Categories.Returns(_categoryRepoMock);
        
        _invoiceService = new CreditCardInvoiceService(_unitOfWorkMock, _processLoggerMock);
    }

    [Fact]
    public async Task GetOrCreateOpenInvoiceAsync_WithExistingInvoice_ShouldReturnExisting()
    {
        // Arrange
        var userId = "user123";
        var accountId = "card123";
        var existingInvoice = new CreditCardInvoice
        {
            Id = "inv123",
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            ReferenceMonth = "2026-02"
        };

        _invoiceRepoMock.GetOpenInvoiceByAccountIdAsync(accountId).Returns(existingInvoice);

        // Act
        var result = await _invoiceService.GetOrCreateOpenInvoiceAsync(userId, accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingInvoice.Id, result.Id);
        Assert.Equal(InvoiceStatus.Open, result.Status);
        await _invoiceRepoMock.DidNotReceive().AddAsync(Arg.Any<CreditCardInvoice>());
    }

    [Fact]
    public async Task GetOrCreateOpenInvoiceAsync_WithoutExisting_ShouldCreateNew()
    {
        // Arrange
        var userId = "user123";
        var accountId = "card123";
        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Type = AccountType.CreditCard,
            InvoiceClosingDay = 10,
            InvoiceDueDayOffset = 7
        };

        _invoiceRepoMock.GetOpenInvoiceByAccountIdAsync(accountId).Returns((CreditCardInvoice?)null);
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);

        // Act
        var result = await _invoiceService.GetOrCreateOpenInvoiceAsync(userId, accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InvoiceStatus.Open, result.Status);
        await _invoiceRepoMock.Received(1).AddAsync(Arg.Any<CreditCardInvoice>());
    }

    [Fact]
    public async Task CloseInvoiceAsync_ShouldCloseInvoiceAndCreateNew()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var accountId = "card123";
        
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            TotalAmount = 500m,
            RemainingAmount = 500m
        };

        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Type = AccountType.CreditCard,
            InvoiceClosingDay = 10,
            InvoiceDueDayOffset = 7
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>());

        // Act
        var result = await _invoiceService.CloseInvoiceAsync(userId, invoiceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(InvoiceStatus.Closed, result.Status);
        // UpdateAsync is called twice: once for recalculation, once for closing
        await _invoiceRepoMock.Received(2).UpdateAsync(Arg.Any<CreditCardInvoice>());
        await _invoiceRepoMock.Received(1).AddAsync(Arg.Any<CreditCardInvoice>()); // New open invoice
    }

    [Fact]
    public async Task PayInvoiceAsync_WithFullPayment_ShouldMarkAsPaid()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            RemainingAmount = 1000m
        };

        var payFromAccount = new Account
        {
            Id = "checking123",
            UserId = userId,
            Type = AccountType.Checking,
            Balance = 2000m
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 1000m,
            PaymentDate = DateTime.Today,
            Description = "Full payment"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("checking123").Returns(payFromAccount);
        _accountRepoMock.GetByIdAsync("card123").Returns(new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = 1000m,
            CommittedCredit = 1000m
        });

        // Act
        await _invoiceService.PayInvoiceAsync(userId, request);

        // Assert
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i => 
            i.Status == InvoiceStatus.Paid && 
            i.PaidAmount == 1000m && 
            i.RemainingAmount == 0m));
    }

    [Fact]
    public async Task PayPartialInvoiceAsync_ShouldMarkAsPartiallyPaid()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            RemainingAmount = 1000m
        };

        var payFromAccount = new Account
        {
            Id = "checking123",
            UserId = userId,
            Type = AccountType.Checking,
            Balance = 500m
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 400m,
            PaymentDate = DateTime.Today,
            Description = "Partial payment"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("checking123").Returns(payFromAccount);
        _accountRepoMock.GetByIdAsync("card123").Returns(new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = 1000m,
            CommittedCredit = 1000m
        });

        // Act
        await _invoiceService.PayPartialInvoiceAsync(userId, request);

        // Assert
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i => 
            i.Status == InvoiceStatus.PartiallyPaid && 
            i.PaidAmount == 400m && 
            i.RemainingAmount == 600m));
    }

    [Fact]
    public async Task DetermineInvoiceForTransactionAsync_BeforeClosingDay_ShouldReturnCurrentMonthInvoice()
    {
        // Arrange
        var userId = "user123";
        var accountId = "card123";
        var transactionDate = new DateTime(2026, 2, 5); // Dia 5
        
        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Type = AccountType.CreditCard,
            InvoiceClosingDay = 10, // Fecha dia 10
            InvoiceDueDayOffset = 7
        };

        var expectedInvoice = new CreditCardInvoice
        {
            Id = "inv202602",
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            ReferenceMonth = "2026-02"
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _invoiceRepoMock.GetByReferenceMonthAsync(accountId, "2026-02").Returns(expectedInvoice);

        // Act
        var result = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, accountId, transactionDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2026-02", result.ReferenceMonth);
    }

    [Fact]
    public async Task DetermineInvoiceForTransactionAsync_AfterClosingDay_ShouldReturnNextMonthInvoice()
    {
        // Arrange
        var userId = "user123";
        var accountId = "card123";
        var transactionDate = new DateTime(2026, 2, 15); // Dia 15 (depois do fechamento dia 10)
        
        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Type = AccountType.CreditCard,
            InvoiceClosingDay = 10,
            InvoiceDueDayOffset = 7
        };

        var expectedInvoice = new CreditCardInvoice
        {
            Id = "inv202603",
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            ReferenceMonth = "2026-03"
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _invoiceRepoMock.GetByReferenceMonthAsync(accountId, "2026-03").Returns(expectedInvoice);

        // Act
        var result = await _invoiceService.DetermineInvoiceForTransactionAsync(userId, accountId, transactionDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("2026-03", result.ReferenceMonth);
    }

    [Fact]
    public async Task RecalculateInvoiceTotalAsync_ShouldSumAllTransactions()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            UserId = userId,
            TotalAmount = 0m,
            PaidAmount = 0m,
            RemainingAmount = 0m
        };

        var transactions = new List<Transaction>
        {
            new Transaction { Id = "t1", InvoiceId = invoiceId, Amount = 100m, Type = TransactionType.Expense, IsDeleted = false },
            new Transaction { Id = "t2", InvoiceId = invoiceId, Amount = 200m, Type = TransactionType.Expense, IsDeleted = false },
            new Transaction { Id = "t3", InvoiceId = invoiceId, Amount = 50m, Type = TransactionType.Expense, IsDeleted = true }, // Deleted
            new Transaction { Id = "t4", InvoiceId = invoiceId, Amount = 150m, Type = TransactionType.Income, IsDeleted = false } // Income (not counted)
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _transactionRepoMock.GetAllAsync().Returns(transactions);

        // Act
        await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);

        // Assert
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i => 
            i.TotalAmount == 300m && // 100 + 200 (t3 deleted, t4 is income)
            i.RemainingAmount == 300m));
    }

    [Fact]
    public async Task GetInvoiceTransactionsAsync_ShouldUseSameEligibilityRuleAsRecalculation()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            UserId = userId,
            AccountId = "card123"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>
        {
            new() { Id = "t1", InvoiceId = invoiceId, AccountId = "card123", CategoryId = "cat1", Amount = 100m, Type = TransactionType.Expense, IsDeleted = false, Date = new DateTime(2026, 4, 1) },
            new() { Id = "t2", InvoiceId = invoiceId, AccountId = "card123", CategoryId = "cat1", Amount = 50m, Type = TransactionType.Transfer, IsDeleted = false, Date = new DateTime(2026, 4, 2) },
            new() { Id = "t3", InvoiceId = invoiceId, AccountId = "card123", CategoryId = "cat1", Amount = 25m, Type = TransactionType.Expense, IsDeleted = true, Date = new DateTime(2026, 4, 3) }
        });
        _accountRepoMock.GetAllAsync().Returns(new List<Account> { new() { Id = "card123", UserId = userId, Name = "Cartão", Color = "#00C896" } });
        _accountRepoMock.GetByIdAsync("card123").Returns(new Account { Id = "card123", UserId = userId, Name = "Cartão", Color = "#00C896" });
        _categoryRepoMock.GetAllAsync().Returns(new List<Category> { new() { Id = "cat1", UserId = userId, Name = "Mercado", Color = "#ef4444" } });

        // Act
        var transactions = (await _invoiceService.GetInvoiceTransactionsAsync(userId, invoiceId)).ToList();
        await _invoiceService.RecalculateInvoiceTotalAsync(userId, invoiceId);

        // Assert
        Assert.Single(transactions);
        Assert.Equal(100m, transactions.Sum(t => t.Amount));
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i => i.TotalAmount == 100m));
    }

    [Fact]
    public async Task PayInvoiceAsync_ShouldUpdateBalancesCreateTransferAndReleaseCommittedCredit()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            RemainingAmount = 1000m,
            ReferenceMonth = "2026-04"
        };

        var payFromAccount = new Account
        {
            Id = "checking123",
            UserId = userId,
            Type = AccountType.Checking,
            Balance = 2000m,
            Currency = "BRL"
        };

        var creditCard = new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = -1000m,
            CreditLimit = 5000m,
            CommittedCredit = 1000m,
            Currency = "BRL"
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 1000m,
            PaymentDate = new DateTime(2026, 4, 20),
            Description = "Full payment"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("checking123").Returns(payFromAccount);
        _accountRepoMock.GetByIdAsync("card123").Returns(creditCard);

        // Act
        await _invoiceService.PayInvoiceAsync(userId, request);

        // Assert
        Assert.Equal(1000m, payFromAccount.Balance);
        Assert.Equal(0m, creditCard.Balance);
        Assert.Equal(0m, creditCard.CommittedCredit);
        await _transactionRepoMock.Received(1).AddAsync(Arg.Is<Transaction>(t =>
            t.Type == TransactionType.Transfer &&
            t.AccountId == "checking123" &&
            t.ToAccountId == "card123" &&
            t.Amount == 1000m));
    }

    [Fact]
    public async Task PayPartialInvoiceAsync_ShouldReleaseCommittedCreditByPaidAmount()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            RemainingAmount = 1000m,
            ReferenceMonth = "2026-04"
        };

        var payFromAccount = new Account
        {
            Id = "checking123",
            UserId = userId,
            Type = AccountType.Checking,
            Balance = 700m,
            Currency = "BRL"
        };

        var creditCard = new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = -1000m,
            CreditLimit = 5000m,
            CommittedCredit = 1000m,
            Currency = "BRL"
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 400m,
            PaymentDate = new DateTime(2026, 4, 20),
            Description = "Partial payment"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("checking123").Returns(payFromAccount);
        _accountRepoMock.GetByIdAsync("card123").Returns(creditCard);

        // Act
        await _invoiceService.PayPartialInvoiceAsync(userId, request);

        // Assert
        Assert.Equal(300m, payFromAccount.Balance);
        Assert.Equal(-600m, creditCard.Balance);
        Assert.Equal(600m, creditCard.CommittedCredit);
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i =>
            i.Status == InvoiceStatus.PartiallyPaid &&
            i.PaidAmount == 400m &&
            i.RemainingAmount == 600m));
    }

    [Fact]
    public async Task ReconcileCreditCardDataAsync_ShouldRecomputeCommittedCreditFromInvoicesAndSchedules()
    {
        // Arrange
        var userId = "user123";
        var card = new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = 50m,
            CommittedCredit = 0m,
            CreditLimit = 5000m
        };

        var invoice = new CreditCardInvoice
        {
            Id = "inv123",
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 0m,
            PaidAmount = 100m,
            RemainingAmount = 0m
        };

        _accountRepoMock.GetAllAsync().Returns(new List<Account> { card });
        _invoiceRepoMock.GetByAccountIdAsync("card123").Returns(new List<CreditCardInvoice> { invoice });
        _invoiceRepoMock.GetByIdAsync("inv123").Returns(invoice);
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>
        {
            new() { Id = "t1", InvoiceId = "inv123", AccountId = "card123", Amount = 200m, Type = TransactionType.Expense, IsDeleted = false }
        });
        _unitOfWorkMock.RecurringTransactions.Returns(Substitute.For<IRepository<RecurringTransaction>>());
        _unitOfWorkMock.RecurringTransactions.GetAllAsync().Returns(new List<RecurringTransaction>
        {
            new() { Id = "rec1", UserId = userId, AccountId = "card123", Amount = 150m, IsActive = true, IsInstallmentSchedule = true }
        });

        // Act
        var summary = await _invoiceService.ReconcileCreditCardDataAsync(userId);

        // Assert
        Assert.Equal(1, summary.AccountsProcessed);
        Assert.Equal(1, summary.InvoicesRecalculated);
        Assert.Equal(250m, card.CommittedCredit);
    }

    [Fact]
    public async Task CreateHistoryInvoiceAsync_ShouldCreatePaidInvoiceAndLinkTransactions()
    {
        // Arrange
        var userId = "user123";
        var accountId = "card123";
        var account = new Account
        {
            Id = accountId,
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = -500m // Debt
        };

        var oldTransactions = new List<Transaction>
        {
            new Transaction { Id = "t1", AccountId = accountId, Date = DateTime.Today.AddDays(-30), InvoiceId = null, IsDeleted = false },
            new Transaction { Id = "t2", AccountId = accountId, Date = DateTime.Today.AddDays(-20), InvoiceId = null, IsDeleted = false },
            new Transaction { Id = "t3", AccountId = accountId, Date = DateTime.Today, InvoiceId = null, IsDeleted = false } // Today (not included)
        };

        _accountRepoMock.GetByIdAsync(accountId).Returns(account);
        _transactionRepoMock.GetAllAsync().Returns(oldTransactions);

        // Act
        var result = await _invoiceService.CreateHistoryInvoiceAsync(userId, accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HISTORY", result.ReferenceMonth);
        Assert.Equal(InvoiceStatus.Paid, result.Status);
        await _invoiceRepoMock.Received(1).AddAsync(Arg.Is<CreditCardInvoice>(i => i.ReferenceMonth == "HISTORY"));
        await _transactionRepoMock.Received(2).UpdateAsync(Arg.Any<Transaction>()); // t1 and t2 (not t3)
    }

    [Fact]
    public async Task GetPendingInvoicesAsync_ShouldReturnClosedUnpaidInvoices()
    {
        // Arrange
        var userId = "user123";
        var invoices = new List<CreditCardInvoice>
        {
            new CreditCardInvoice { Id = "inv1", UserId = userId, AccountId = "card1", Status = InvoiceStatus.Open },
            new CreditCardInvoice { Id = "inv2", UserId = userId, AccountId = "card1", Status = InvoiceStatus.Closed, RemainingAmount = 500m },
            new CreditCardInvoice { Id = "inv3", UserId = userId, AccountId = "card1", Status = InvoiceStatus.Paid },
            new CreditCardInvoice { Id = "inv4", UserId = userId, AccountId = "card1", Status = InvoiceStatus.PartiallyPaid, RemainingAmount = 200m }
        };

        _invoiceRepoMock.GetClosedUnpaidInvoicesAsync(userId).Returns(invoices.Where(i => 
            i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Open).ToList());
        _transactionRepoMock.GetAllAsync().Returns(new List<Transaction>());
        _accountRepoMock.GetByIdAsync(Arg.Any<string>()).Returns(new Account { Name = "Test Card" });

        // Act
        var result = await _invoiceService.GetPendingInvoicesAsync(userId);

        // Assert
        Assert.Equal(2, result.Count()); // inv2 (Closed) and inv4 (PartiallyPaid)
    }

    [Fact]
    public async Task PayInvoiceAsync_AlreadyPaid_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            UserId = userId,
            Status = InvoiceStatus.Paid, // Already paid
            TotalAmount = 1000m,
            PaidAmount = 1000m,
            RemainingAmount = 0m
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 100m,
            PaymentDate = DateTime.Today
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _invoiceService.PayInvoiceAsync(userId, request));
    }

    [Fact]
    public async Task PayInvoiceAsync_FromCreditCard_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            RemainingAmount = 1000m
        };

        var creditCardAccount = new Account
        {
            Id = "cc123",
            UserId = userId,
            Type = AccountType.CreditCard // Cannot pay from credit card!
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "cc123",
            Amount = 1000m,
            PaymentDate = DateTime.Today
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("cc123").Returns(creditCardAccount);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.PayInvoiceAsync(userId, request));
    }

    [Fact]
    public async Task PayInvoiceAsync_AmountExceedsRemaining_ShouldCapAtRemainingAndMarkPaid()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 500m,
            PaidAmount = 300m,
            RemainingAmount = 200m
        };

        var payFromAccount = new Account
        {
            Id = "checking123",
            UserId = userId,
            Type = AccountType.Checking,
            Balance = 5000m
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 200m, // Exact remaining
            PaymentDate = DateTime.Today,
            Description = "Final payment"
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);
        _accountRepoMock.GetByIdAsync("checking123").Returns(payFromAccount);
        _accountRepoMock.GetByIdAsync("card123").Returns(new Account
        {
            Id = "card123",
            UserId = userId,
            Type = AccountType.CreditCard,
            Balance = 200m,
            CommittedCredit = 200m
        });

        // Act
        await _invoiceService.PayInvoiceAsync(userId, request);

        // Assert
        await _invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i =>
            i.Status == InvoiceStatus.Paid &&
            i.PaidAmount == 500m &&
            i.RemainingAmount == 0m));
    }

    [Fact]
    public async Task PayPartialInvoiceAsync_AmountExceedsRemaining_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed,
            TotalAmount = 1000m,
            PaidAmount = 800m,
            RemainingAmount = 200m
        };

        var request = new PayInvoiceRequestDto
        {
            InvoiceId = invoiceId,
            PayFromAccountId = "checking123",
            Amount = 500m, // Exceeds remaining 200
            PaymentDate = DateTime.Today
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.PayPartialInvoiceAsync(userId, request));
        Assert.Contains("remaining amount", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseInvoiceAsync_AlreadyClosed_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var invoiceId = "inv123";
        var invoice = new CreditCardInvoice
        {
            Id = invoiceId,
            AccountId = "card123",
            UserId = userId,
            Status = InvoiceStatus.Closed // Already closed
        };

        _invoiceRepoMock.GetByIdAsync(invoiceId).Returns(invoice);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _invoiceService.CloseInvoiceAsync(userId, invoiceId));
    }

    [Fact]
    public async Task GetOrCreateOpenInvoiceAsync_WithNoAccount_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var accountId = "nonexistent";

        _invoiceRepoMock.GetOpenInvoiceByAccountIdAsync(accountId).Returns((CreditCardInvoice?)null);
        _accountRepoMock.GetByIdAsync(accountId).Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _invoiceService.GetOrCreateOpenInvoiceAsync(userId, accountId));
    }
}

