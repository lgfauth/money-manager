using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class ReportServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IReportService _reportService;

    public ReportServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _reportService = new ReportService(_unitOfWorkMock);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldCalculateTotals()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 5000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 15)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 2000m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 20)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 25)
            }
        };

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetMonthlySummaryAsync(userId, month);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5000m, result.TotalIncome);
        Assert.Equal(3000m, result.TotalExpense);
        Assert.Equal(2000m, result.Balance);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ShouldFilterByMonth()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 15)
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 2000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 2, 15) // Different month
            }
        };

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetMonthlySummaryAsync(userId, month);

        // Assert
        Assert.Equal(1000m, result.TotalIncome);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldGroupExpenses()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 300m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 200m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 12),
                CategoryId = "cat2"
            }
        };

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        var cat1Expense = resultList.First(r => r.CategoryId == "cat1");
        Assert.Equal(800m, cat1Expense.Total);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldFilterDeletedTransactions()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1",
                IsDeleted = false
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 300m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat1",
                IsDeleted = true
            }
        };

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        var cat1Expense = resultList.First(r => r.CategoryId == "cat1");
        Assert.Equal(500m, cat1Expense.Total);
    }

    [Fact]
    public async Task GetExpensesByCategoryAsync_ShouldExcludeIncomeTransactions()
    {
        // Arrange
        var userId = "user123";
        var month = "2024-01";
        
        var transactions = new List<Transaction>
        {
            new Transaction 
            { 
                UserId = userId, 
                Amount = 1000m, 
                Type = TransactionType.Income,
                Date = new DateTime(2024, 1, 5),
                CategoryId = "cat1"
            },
            new Transaction 
            { 
                UserId = userId, 
                Amount = 500m, 
                Type = TransactionType.Expense,
                Date = new DateTime(2024, 1, 10),
                CategoryId = "cat2"
            }
        };

        var transactionRepo = Substitute.For<ITransactionRepository>();
        transactionRepo.GetAllAsync().Returns(transactions);
        _unitOfWorkMock.Transactions.Returns(transactionRepo);

        // Act
        var result = await _reportService.GetExpensesByCategoryAsync(userId, month);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("cat2", resultList.First().CategoryId);
    }

    private const string OverviewUserId = "user123";

    private sealed class OverviewScenario
    {
        public List<Account> Accounts { get; } = [new Account { Id = "acc1", UserId = OverviewUserId, Name = "Conta", Balance = 0m }];
        public List<CreditCard> Cards { get; } = [];
        public List<Category> Categories { get; } =
        [
            new Category { Id = "food", UserId = OverviewUserId, Name = "Alimentação", Color = "#f00" },
            new Category { Id = "salary", UserId = OverviewUserId, Name = "Salário", Color = "#0f0" }
        ];
        public List<CreditCardInvoice> Invoices { get; } = [];
        public List<Transaction> Transactions { get; } = [];
        public List<CreditCardTransaction> CardItems { get; } = [];
        public List<Budget> Budgets { get; } = [];
    }

    private void SetupOverview(OverviewScenario scenario)
    {
        var settingsRepository = Substitute.For<IRepository<UserSettings>>();
        settingsRepository.GetByUserIdAsync(OverviewUserId).Returns(new List<UserSettings>());
        _unitOfWorkMock.UserSettings.Returns(settingsRepository);

        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository.GetByUserIdAsync(OverviewUserId).Returns(scenario.Accounts);
        _unitOfWorkMock.Accounts.Returns(accountRepository);

        var cardRepository = Substitute.For<ICreditCardRepository>();
        cardRepository.GetByUserAsync(OverviewUserId).Returns(scenario.Cards);
        _unitOfWorkMock.CreditCards.Returns(cardRepository);

        var categoryRepository = Substitute.For<IRepository<Category>>();
        categoryRepository.GetByUserIdAsync(OverviewUserId).Returns(scenario.Categories);
        _unitOfWorkMock.Categories.Returns(categoryRepository);

        var budgetRepository = Substitute.For<IRepository<Budget>>();
        budgetRepository.GetByUserIdAsync(OverviewUserId).Returns(scenario.Budgets);
        _unitOfWorkMock.Budgets.Returns(budgetRepository);

        var invoiceRepository = Substitute.For<ICreditCardInvoiceRepository>();
        invoiceRepository.GetByUserAsync(OverviewUserId).Returns(scenario.Invoices);
        _unitOfWorkMock.CreditCardInvoices.Returns(invoiceRepository);

        var transactionRepository = Substitute.For<ITransactionRepository>();
        transactionRepository.GetByUserFromDateAsync(OverviewUserId, Arg.Any<DateTime>())
            .Returns(call => scenario.Transactions.Where(t => t.Date >= call.Arg<DateTime>()).ToList());
        _unitOfWorkMock.Transactions.Returns(transactionRepository);

        var cardTransactionRepository = Substitute.For<ICreditCardTransactionRepository>();
        // Reproduz o filtro do repositório: à vista a partir de fromDate, parceladas a partir da janela retroativa
        cardTransactionRepository.GetByUserFromPurchaseDateAsync(OverviewUserId, Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(call =>
            {
                var fromDate = call.ArgAt<DateTime>(1);
                var installmentsFromDate = call.ArgAt<DateTime>(2);
                return scenario.CardItems
                    .Where(t => t.PurchaseDate >= fromDate
                                || (t.TotalInstallments > 1 && t.PurchaseDate >= installmentsFromDate))
                    .ToList();
            });
        cardTransactionRepository.GetByInvoicesAsync(OverviewUserId, Arg.Any<IReadOnlyCollection<string>>())
            .Returns(call => scenario.CardItems.Where(t => call.Arg<IReadOnlyCollection<string>>().Contains(t.InvoiceId)).ToList());
        _unitOfWorkMock.CreditCardTransactions.Returns(cardTransactionRepository);
    }

    private static ReportOverviewRequestDto February2024(ReportBasis basis = ReportBasis.Accrual) => new()
    {
        StartDate = new DateTime(2024, 2, 1),
        EndDate = new DateTime(2024, 2, 29),
        Basis = basis
    };

    private static List<CreditCardTransaction> CreateInstallments(string cardId, string invoiceId, DateTime purchaseDate, decimal installmentAmount, int totalInstallments, string? categoryId)
        => Enumerable.Range(1, totalInstallments)
            .Select(number => new CreditCardTransaction
            {
                UserId = OverviewUserId,
                CreditCardId = cardId,
                InvoiceId = invoiceId,
                Description = "Compra parcelada",
                CategoryId = categoryId,
                PurchaseDate = purchaseDate,
                InstallmentAmount = installmentAmount,
                TotalAmount = installmentAmount * totalInstallments,
                InstallmentNumber = number,
                TotalInstallments = totalInstallments
            })
            .ToList();

    [Fact]
    public async Task GetOverviewAsync_Accrual_ShouldCountCardInstallmentsInTheirMonthAndExcludeInvoicePayment()
    {
        var scenario = new OverviewScenario();
        scenario.Cards.Add(new CreditCard { Id = "card1", UserId = OverviewUserId, Name = "Cartão" });
        scenario.CardItems.AddRange(CreateInstallments("card1", "inv1", new DateTime(2024, 1, 10), 100m, 3, "food"));
        scenario.Invoices.Add(new CreditCardInvoice { Id = "inv1", CreditCardId = "card1", PaymentTransactionId = "pay1", Status = InvoiceStatus.Paid });
        scenario.Transactions.Add(new Transaction { Id = "pay1", AccountId = "acc1", Type = TransactionType.Expense, Amount = 100m, Date = new DateTime(2024, 2, 15) });
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(100m, result.Totals.Expense);
        var food = Assert.Single(result.ExpensesByCategory);
        Assert.Equal("food", food.CategoryId);
        Assert.Equal(100m, food.PreviousTotal);
        Assert.Equal(1, result.DataQuality.ExcludedInvoicePaymentsCount);
        Assert.Equal("Cartão", Assert.Single(result.ByCreditCard).CreditCardName);
        Assert.Equal("2/3", Assert.Single(result.TopExpenses).InstallmentLabel);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldExcludeTransfersAndCancelledTransactions()
    {
        var scenario = new OverviewScenario();
        scenario.Transactions.AddRange(
        [
            new Transaction { AccountId = "acc1", Type = TransactionType.Income, Amount = 5000m, Date = new DateTime(2024, 2, 5), CategoryId = "salary" },
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 200m, Date = new DateTime(2024, 2, 6), CategoryId = "food" },
            new Transaction { AccountId = "acc1", Type = TransactionType.Transfer, Amount = 1000m, Date = new DateTime(2024, 2, 7) },
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 700m, Date = new DateTime(2024, 2, 8), OpenBankingCategoryId = "04020000" },
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 300m, Date = new DateTime(2024, 2, 9), Status = TransactionStatus.Cancelled }
        ]);
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(5000m, result.Totals.Income);
        Assert.Equal(200m, result.Totals.Expense);
        Assert.Equal(96m, result.Totals.SavingsRate);
        Assert.Equal(2, result.DataQuality.ExcludedTransfersCount);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldKeepBankInvoicePaymentWhenUserHasNoCreditCards()
    {
        var scenario = new OverviewScenario();
        scenario.Transactions.Add(new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 900m, Date = new DateTime(2024, 2, 10), OpenBankingCategoryId = "05100000" });
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(900m, result.Totals.Expense);
        Assert.Equal(1, result.DataQuality.UncategorizedCount);
    }

    [Fact]
    public async Task GetOverviewAsync_Cash_ShouldSpreadPaidInvoiceAcrossCategoriesOnPaymentDate()
    {
        var scenario = new OverviewScenario();
        scenario.Cards.Add(new CreditCard { Id = "card1", UserId = OverviewUserId, Name = "Cartão" });
        scenario.Invoices.Add(new CreditCardInvoice
        {
            Id = "inv1", CreditCardId = "card1", Status = InvoiceStatus.Paid,
            PaidAt = new DateTime(2024, 2, 10), PaidAmount = 200m, PaymentTransactionId = "pay1"
        });
        scenario.CardItems.AddRange(CreateInstallments("card1", "inv1", new DateTime(2024, 1, 5), 300m, 1, "food"));
        scenario.CardItems.AddRange(CreateInstallments("card1", "inv1", new DateTime(2024, 1, 6), 100m, 1, null));
        scenario.Transactions.Add(new Transaction { Id = "pay1", AccountId = "acc1", Type = TransactionType.Expense, Amount = 200m, Date = new DateTime(2024, 2, 10) });
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024(ReportBasis.Cash));

        Assert.Equal(200m, result.Totals.Expense);
        Assert.Equal(150m, result.ExpensesByCategory.Single(c => c.CategoryId == "food").Total);
        Assert.Equal(50m, result.ExpensesByCategory.Single(c => c.CategoryId == "uncategorized").Total);
    }

    [Fact]
    public async Task GetOverviewAsync_Cash_ShouldUseInvoiceAmountWhenInvoiceHasNoItems()
    {
        var scenario = new OverviewScenario();
        scenario.Cards.Add(new CreditCard { Id = "card1", UserId = OverviewUserId, Name = "Cartão" });
        scenario.Invoices.Add(new CreditCardInvoice
        {
            Id = "inv1", CreditCardId = "card1", Status = InvoiceStatus.Paid, ReferenceMonth = "2024-01",
            PaidAt = new DateTime(2024, 2, 10), TotalAmount = 450m
        });
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024(ReportBasis.Cash));

        Assert.Equal(450m, result.Totals.Expense);
        Assert.Equal("Fatura Cartão (2024-01)", Assert.Single(result.TopExpenses).Description);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldCompareWithEquivalentPreviousMonth()
    {
        var scenario = new OverviewScenario();
        scenario.Transactions.AddRange(
        [
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 80m, Date = new DateTime(2024, 1, 31), CategoryId = "food" },
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 120m, Date = new DateTime(2024, 2, 29), CategoryId = "food" }
        ]);
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(new DateTime(2024, 1, 1), result.PreviousPeriod.StartDate);
        Assert.Equal(new DateTime(2024, 1, 31), result.PreviousPeriod.EndDate);
        Assert.Equal(80m, result.PreviousTotals.Expense);
        Assert.Equal(120m, result.Totals.Expense);
        Assert.Equal("2024-02", Assert.Single(result.MonthlyTrends).Month);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldReconstructDailyBalanceFromCurrentBalance()
    {
        var scenario = new OverviewScenario();
        scenario.Accounts[0].Balance = 1000m;
        scenario.Transactions.AddRange(
        [
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 200m, Date = new DateTime(2024, 2, 10) },
            new Transaction { AccountId = "acc1", Type = TransactionType.Income, Amount = 500m, Date = new DateTime(2024, 3, 5) }
        ]);
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(29, result.DailyPoints.Count);
        Assert.Equal(700m, result.DailyPoints.Single(p => p.Date == new DateTime(2024, 2, 9)).Balance);
        Assert.Equal(500m, result.DailyPoints.Single(p => p.Date == new DateTime(2024, 2, 10)).Balance);
        Assert.Equal(500m, result.DailyPoints[^1].Balance);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldSeparateOtherCurrencies()
    {
        var scenario = new OverviewScenario();
        scenario.Transactions.AddRange(
        [
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 100m, Date = new DateTime(2024, 2, 10) },
            new Transaction { AccountId = "acc1", Type = TransactionType.Expense, Amount = 30m, Currency = "USD", Date = new DateTime(2024, 2, 11) }
        ]);
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(100m, result.Totals.Expense);
        var usd = Assert.Single(result.OtherCurrencies);
        Assert.Equal("USD", usd.Currency);
        Assert.Equal(30m, usd.Expense);
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldListUpcomingInstallments()
    {
        var scenario = new OverviewScenario();
        scenario.Cards.Add(new CreditCard { Id = "card1", UserId = OverviewUserId, Name = "Cartão" });
        scenario.CardItems.AddRange(CreateInstallments("card1", "inv1", DateTime.UtcNow.Date.AddDays(-1), 50m, 4, "food"));
        SetupOverview(scenario);

        var result = await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        Assert.Equal(3, result.UpcomingInstallments.Count);
        Assert.All(result.UpcomingInstallments, month => Assert.Equal(50m, month.Total));
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldLoadOldCardPurchasesOnlyWhenInstallments()
    {
        // Arrange
        SetupOverview(new OverviewScenario());

        // Act
        await _reportService.GetOverviewAsync(OverviewUserId, February2024());

        // Assert
        await _unitOfWorkMock.CreditCardTransactions.Received(1).GetByUserFromPurchaseDateAsync(
            OverviewUserId, new DateTime(2024, 1, 1), new DateTime(2020, 1, 1));
    }

    [Fact]
    public async Task GetOverviewAsync_ShouldRejectEndDateBeforeStartDate()
    {
        var request = new ReportOverviewRequestDto { StartDate = new DateTime(2024, 2, 10), EndDate = new DateTime(2024, 2, 1) };

        await Assert.ThrowsAsync<ArgumentException>(() => _reportService.GetOverviewAsync(OverviewUserId, request));
    }
}
