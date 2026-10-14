using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class DashboardServiceTests
{
    private const string UserId = "user123";

    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IDashboardService _dashboardService;

    private readonly List<Account> _accounts = [];
    private readonly List<CreditCard> _cards = [];
    private readonly List<CreditCardInvoice> _invoices = [];
    private readonly List<RecurringTransaction> _recurringTransactions = [];
    private readonly List<Category> _categories = [];

    public DashboardServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _dashboardService = new DashboardService(_unitOfWorkMock);

        var settingsRepository = Substitute.For<IRepository<UserSettings>>();
        settingsRepository.GetByUserIdAsync(UserId).Returns(new List<UserSettings>());
        _unitOfWorkMock.UserSettings.Returns(settingsRepository);

        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository.GetByUserIdAsync(UserId).Returns(_ => _accounts);
        _unitOfWorkMock.Accounts.Returns(accountRepository);

        var cardRepository = Substitute.For<ICreditCardRepository>();
        cardRepository.GetByUserAsync(UserId).Returns(_ => _cards);
        _unitOfWorkMock.CreditCards.Returns(cardRepository);

        var invoiceRepository = Substitute.For<ICreditCardInvoiceRepository>();
        invoiceRepository.GetByUserAsync(UserId).Returns(_ => _invoices);
        _unitOfWorkMock.CreditCardInvoices.Returns(invoiceRepository);

        var recurringRepository = Substitute.For<IRepository<RecurringTransaction>>();
        recurringRepository.GetByUserIdAsync(UserId).Returns(_ => _recurringTransactions);
        _unitOfWorkMock.RecurringTransactions.Returns(recurringRepository);

        var categoryRepository = Substitute.For<IRepository<Category>>();
        categoryRepository.GetByUserIdAsync(UserId).Returns(_ => _categories);
        _unitOfWorkMock.Categories.Returns(categoryRepository);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldSubtractUnpaidInvoicesFromAccountsBalance()
    {
        // Arrange
        _accounts.Add(new Account { Id = "acc1", Balance = 3000m });
        _accounts.Add(new Account { Id = "acc2", Balance = 500m, IsDeleted = true });
        _cards.Add(new CreditCard { Id = "card1", Name = "Cartão" });
        _invoices.AddRange(
        [
            new CreditCardInvoice { CreditCardId = "card1", Status = InvoiceStatus.Closed, TotalAmount = 800m },
            new CreditCardInvoice { CreditCardId = "card1", Status = InvoiceStatus.Pending, TotalAmount = 200m },
            new CreditCardInvoice { CreditCardId = "card1", Status = InvoiceStatus.Paid, TotalAmount = 900m }
        ]);

        // Act
        var result = await _dashboardService.GetSummaryAsync(UserId);

        // Assert
        Assert.Equal(3000m, result.AccountsBalance);
        Assert.Equal(1000m, result.CreditCardDebt);
        Assert.Equal(2000m, result.NetWorth);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldIgnoreDeletedCardsAndOtherCurrencies()
    {
        // Arrange
        _accounts.Add(new Account { Id = "acc1", Balance = 1000m });
        _accounts.Add(new Account { Id = "acc2", Balance = 400m, Currency = "USD" });
        _cards.Add(new CreditCard { Id = "deleted", IsDeleted = true });
        _invoices.Add(new CreditCardInvoice { CreditCardId = "deleted", Status = InvoiceStatus.Open, TotalAmount = 300m });

        // Act
        var result = await _dashboardService.GetSummaryAsync(UserId);

        // Assert
        Assert.Equal(1000m, result.NetWorth);
        Assert.Empty(result.UpcomingInvoices);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldListUpcomingInvoicesByDueDateAndFlagOverdue()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        _cards.Add(new CreditCard { Id = "card1", Name = "Cartão" });
        _invoices.AddRange(
        [
            new CreditCardInvoice { Id = "open", CreditCardId = "card1", Status = InvoiceStatus.Open, TotalAmount = 150m, DueDate = today.AddDays(30) },
            new CreditCardInvoice { Id = "late", CreditCardId = "card1", Status = InvoiceStatus.Closed, TotalAmount = 700m, DueDate = today.AddDays(-2) },
            new CreditCardInvoice { Id = "future", CreditCardId = "card1", Status = InvoiceStatus.Pending, TotalAmount = 90m, DueDate = today.AddDays(60) },
            new CreditCardInvoice { Id = "empty", CreditCardId = "card1", Status = InvoiceStatus.Open, TotalAmount = 0m, DueDate = today.AddDays(5) }
        ]);

        // Act
        var result = await _dashboardService.GetSummaryAsync(UserId);

        // Assert
        Assert.Equal(["late", "open"], result.UpcomingInvoices.Select(invoice => invoice.InvoiceId));
        Assert.True(result.UpcomingInvoices[0].IsOverdue);
        Assert.False(result.UpcomingInvoices[1].IsOverdue);
    }

    [Fact]
    public async Task GetSummaryAsync_ShouldListOnlyActiveRecurringWithinWindow()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        _accounts.Add(new Account { Id = "acc1", Name = "Conta" });
        _categories.Add(new Category { Id = "rent", Name = "Moradia", Color = "#123456" });
        _recurringTransactions.AddRange(
        [
            new RecurringTransaction { Id = "soon", AccountId = "acc1", CategoryId = "rent", Description = "Aluguel", Amount = 1500m, Type = TransactionType.Expense, NextOccurrenceDate = today.AddDays(3) },
            new RecurringTransaction { Id = "far", AccountId = "acc1", NextOccurrenceDate = today.AddDays(40) },
            new RecurringTransaction { Id = "inactive", AccountId = "acc1", IsActive = false, NextOccurrenceDate = today.AddDays(1) },
            new RecurringTransaction { Id = "ended", AccountId = "acc1", NextOccurrenceDate = today.AddDays(2), EndDate = today.AddDays(1) }
        ]);

        // Act
        var result = await _dashboardService.GetSummaryAsync(UserId);

        // Assert
        var upcoming = Assert.Single(result.UpcomingRecurring);
        Assert.Equal("soon", upcoming.Id);
        Assert.Equal("Moradia", upcoming.CategoryName);
        Assert.Equal("Conta", upcoming.AccountName);
    }
}
