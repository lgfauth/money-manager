using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class TransactionServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepository<Account> _accountRepoMock;
    private readonly ITransactionRepository _transactionRepoMock;
    private readonly ICreditCardInvoiceService _invoiceServiceMock;
    private readonly IProcessLogger _processLoggerMock;
    private readonly ITransactionService _transactionService;

    public TransactionServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _accountRepoMock = Substitute.For<IRepository<Account>>();
        _transactionRepoMock = Substitute.For<ITransactionRepository>();
        _invoiceServiceMock = Substitute.For<ICreditCardInvoiceService>();
        _processLoggerMock = Substitute.For<IProcessLogger>();

        _unitOfWorkMock.Accounts.Returns(_accountRepoMock);
        _unitOfWorkMock.Transactions.Returns(_transactionRepoMock);

        _transactionRepoMock.AddAsync(Arg.Any<Transaction>()).Returns(x => x.Arg<Transaction>());

        _transactionService = new TransactionService(_unitOfWorkMock, _invoiceServiceMock, _processLoggerMock);
    }

    [Fact]
    public async Task CreateAsync_WithIncome_ShouldIncreaseBalance()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 0, // Income
            Amount = 500m,
            Date = DateTime.UtcNow,
            Description = "Salary",
            Status = 0
        };

        var account = new Account { Id = accountId, UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(1500m, account.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == accountId));
        await _transactionRepoMock.Received(1).AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task CreateAsync_WithExpense_ShouldDecreaseBalance()
    {
        // Arrange
        var userId = "user123";
        var accountId = "acc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 1, // Expense
            Amount = 200m,
            Date = DateTime.UtcNow,
            Description = "Groceries",
            Status = 0
        };

        var account = new Account { Id = accountId, UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync(accountId).Returns(account);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(800m, account.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == accountId));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_ShouldUpdateBothAccounts()
    {
        // Arrange
        var userId = "user123";
        var fromAccountId = "acc123";
        var toAccountId = "acc456";
        var request = new CreateTransactionRequestDto
        {
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = 2, // Transfer
            Amount = 300m,
            Date = DateTime.UtcNow,
            Description = "Transfer",
            Status = 0
        };

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m, Type = AccountType.Checking };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m, Type = AccountType.Checking };

        _accountRepoMock.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _accountRepoMock.GetByIdAsync(toAccountId).Returns(toAccount);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(700m, fromAccount.Balance);
        Assert.Equal(800m, toAccount.Balance);
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == fromAccountId));
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == toAccountId));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_ToCreditCard_ShouldReduceDebt()
    {
        // Arrange
        var userId = "user123";
        var fromAccountId = "acc123";
        var toAccountId = "cc456";
        var request = new CreateTransactionRequestDto
        {
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = 2, // Transfer
            Amount = 300m,
            Date = DateTime.UtcNow,
            Description = "Payment",
            Status = 0
        };

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m, Type = AccountType.Checking };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m, Type = AccountType.CreditCard };

        _accountRepoMock.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _accountRepoMock.GetByIdAsync(toAccountId).Returns(toAccount);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(700m, fromAccount.Balance);
        Assert.Equal(200m, toAccount.Balance); // 500 - 300 = 200 (debt reduced)
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == fromAccountId));
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == toAccountId));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidAccount_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = "invalid",
            Type = 0,
            Amount = 100m,
            Date = DateTime.UtcNow,
            Status = 0
        };

        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateAsync(userId, request));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserTransactions()
    {
        // Arrange
        var userId = "user123";
        var transactions = new List<Transaction>
        {
            new Transaction { Id = "1", UserId = userId, Amount = 100, Type = TransactionType.Income },
            new Transaction { Id = "2", UserId = userId, Amount = 50, Type = TransactionType.Expense },
            new Transaction { Id = "3", UserId = "other", Amount = 200, Type = TransactionType.Income }
        };

        _transactionRepoMock.GetAllAsync().Returns(transactions);

        // Act
        var result = await _transactionService.GetAllAsync(userId);

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnTransaction()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            Amount = 100,
            Type = TransactionType.Income
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(transaction);

        // Act
        var result = await _transactionService.GetByIdAsync(userId, transactionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(transactionId, result.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRevertAndApplyNewImpact()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var existingTransaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            AccountId = "acc123",
            Amount = 100m,
            Type = TransactionType.Income
        };

        var updateRequest = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            CategoryId = "cat123",
            Type = 0,
            Amount = 150m,
            Date = DateTime.UtcNow,
            Description = "Updated",
            Status = 0
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(existingTransaction);

        var account = new Account { Id = "acc123", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act
        var result = await _transactionService.UpdateAsync(userId, transactionId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRevertImpactAndMarkAsDeleted()
    {
        // Arrange
        var userId = "user123";
        var transactionId = "trans123";
        var transaction = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            AccountId = "acc123",
            Amount = 200m,
            Type = TransactionType.Expense
        };

        _transactionRepoMock.GetByIdAsync(transactionId).Returns(transaction);

        var account = new Account { Id = "acc123", UserId = userId, Balance = 800m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act
        await _transactionService.DeleteAsync(userId, transactionId);

        // Assert
        Assert.Equal(1000m, account.Balance); // 800 + 200 reverted
        await _accountRepoMock.Received(1).UpdateAsync(Arg.Is<Account>(a => a.Id == "acc123"));
        await _transactionRepoMock.Received(1).UpdateAsync(Arg.Is<Transaction>(t => t.IsDeleted));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_DestinationFails_ShouldRollbackSourceBalance()
    {
        // Arrange
        var userId = "user123";
        var fromAccountId = "acc123";
        var toAccountId = "acc456";
        var request = new CreateTransactionRequestDto
        {
            AccountId = fromAccountId,
            ToAccountId = toAccountId,
            Type = 2, // Transfer
            Amount = 300m,
            Date = DateTime.UtcNow,
            Description = "Transfer",
            Status = 0
        };

        var fromAccount = new Account { Id = fromAccountId, UserId = userId, Balance = 1000m, Type = AccountType.Checking };
        var toAccount = new Account { Id = toAccountId, UserId = userId, Balance = 500m, Type = AccountType.Checking };

        _accountRepoMock.GetByIdAsync(fromAccountId).Returns(fromAccount);
        _accountRepoMock.GetByIdAsync(toAccountId).Returns(toAccount);

        // Simulate destination update failure
        _accountRepoMock.UpdateAsync(Arg.Is<Account>(a => a.Id == toAccountId))
            .Returns<Account>(_ => throw new Exception("DB error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _transactionService.CreateAsync(userId, request));

        // Source balance should be rolled back to original
        Assert.Equal(1000m, fromAccount.Balance);
    }

    [Fact]
    public async Task CreateAsync_WithClientRequestId_Duplicate_ShouldReturnExistingTransaction()
    {
        // Arrange
        var userId = "user123";
        var clientRequestId = "req-abc-123";
        var existingTransaction = new Transaction
        {
            Id = "trans-existing",
            UserId = userId,
            AccountId = "acc123",
            Amount = 500m,
            Type = TransactionType.Income,
            Description = "Original",
            ClientRequestId = clientRequestId
        };

        var request = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            Type = 0,
            Amount = 500m,
            Date = DateTime.UtcNow,
            Description = "Duplicate attempt",
            Status = 0,
            ClientRequestId = clientRequestId
        };

        _transactionRepoMock.GetByClientRequestIdAsync(userId, clientRequestId).Returns(existingTransaction);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert — should return existing, not create new
        Assert.Equal("trans-existing", result.Id);
        await _transactionRepoMock.DidNotReceive().AddAsync(Arg.Any<Transaction>());
        await _accountRepoMock.DidNotReceive().UpdateAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task CreateAsync_WithClientRequestId_NoDuplicate_ShouldCreateNormally()
    {
        // Arrange
        var userId = "user123";
        var clientRequestId = "req-new-123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            CategoryId = "cat123",
            Type = 0, // Income
            Amount = 200m,
            Date = DateTime.UtcNow,
            Description = "New transaction",
            Status = 0,
            ClientRequestId = clientRequestId
        };

        _transactionRepoMock.GetByClientRequestIdAsync(userId, clientRequestId).Returns((Transaction?)null);

        var account = new Account { Id = "acc123", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(200m, result.Amount);
        Assert.Equal(1200m, account.Balance);
        await _transactionRepoMock.Received(1).AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task CreateAsync_CreditCardExpense_ExceedsCreditLimit_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var accountId = "cc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 1, // Expense
            Amount = 600m,
            Date = DateTime.UtcNow,
            Description = "Big purchase",
            Status = 0
        };

        var creditCard = new Account
        {
            Id = accountId,
            UserId = userId,
            Balance = -400m, // Already has R$400 debt
            Type = AccountType.CreditCard,
            CreditLimit = 500m // Limit is R$500, only R$100 available
        };
        _accountRepoMock.GetByIdAsync(accountId).Returns(creditCard);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionService.CreateAsync(userId, request));

        Assert.Contains("Limite de crédito excedido", ex.Message);
        await _transactionRepoMock.DidNotReceive().AddAsync(Arg.Any<Transaction>());
    }

    [Fact]
    public async Task CreateAsync_CreditCardExpense_WithinLimit_ShouldSucceed()
    {
        // Arrange
        var userId = "user123";
        var accountId = "cc123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 1, // Expense
            Amount = 50m,
            Date = DateTime.UtcNow,
            Description = "Small purchase",
            Status = 0
        };

        var creditCard = new Account
        {
            Id = accountId,
            UserId = userId,
            Balance = -400m,
            Type = AccountType.CreditCard,
            CreditLimit = 500m // R$100 available
        };
        _accountRepoMock.GetByIdAsync(accountId).Returns(creditCard);

        var invoice = new CreditCardInvoice
        {
            Id = "inv123",
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            ReferenceMonth = "2026-03"
        };
        _invoiceServiceMock.DetermineInvoiceForTransactionAsync(userId, accountId, Arg.Any<DateTime>())
            .Returns(invoice);
        _unitOfWorkMock.CreditCardInvoices.Returns(Substitute.For<ICreditCardInvoiceRepository>());

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50m, result.Amount);
        // Credit card: CalculateBalanceImpact inverts for CC (expense → positive impact)
        Assert.Equal(-350m, creditCard.Balance);
    }

    [Fact]
    public async Task CreateAsync_CreditCardExpense_ShouldLinkToInvoice()
    {
        // Arrange
        var userId = "user123";
        var accountId = "cc123";
        var transactionDate = new DateTime(2026, 3, 15);
        var request = new CreateTransactionRequestDto
        {
            AccountId = accountId,
            CategoryId = "cat123",
            Type = 1, // Expense
            Amount = 200m,
            Date = transactionDate,
            Description = "Restaurant",
            Status = 0
        };

        var creditCard = new Account
        {
            Id = accountId,
            UserId = userId,
            Balance = 0m,
            Type = AccountType.CreditCard,
            CreditLimit = 5000m
        };
        _accountRepoMock.GetByIdAsync(accountId).Returns(creditCard);

        var invoice = new CreditCardInvoice
        {
            Id = "inv-march",
            AccountId = accountId,
            UserId = userId,
            Status = InvoiceStatus.Open,
            ReferenceMonth = "2026-03",
            TotalAmount = 100m,
            PaidAmount = 0m,
            RemainingAmount = 100m
        };

        var invoiceRepoMock = Substitute.For<ICreditCardInvoiceRepository>();
        _unitOfWorkMock.CreditCardInvoices.Returns(invoiceRepoMock);
        _invoiceServiceMock.DetermineInvoiceForTransactionAsync(userId, accountId, transactionDate)
            .Returns(invoice);

        // Act
        var result = await _transactionService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        // Invoice total should be updated
        Assert.Equal(300m, invoice.TotalAmount); // 100 + 200
        Assert.Equal(300m, invoice.RemainingAmount);
        await invoiceRepoMock.Received(1).UpdateAsync(Arg.Is<CreditCardInvoice>(i => i.Id == "inv-march"));
        // Transaction should have been created with InvoiceId
        await _transactionRepoMock.Received(1).AddAsync(Arg.Is<Transaction>(t => t.InvoiceId == "inv-march"));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_MissingToAccountId_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            Type = 2, // Transfer
            Amount = 100m,
            Date = DateTime.UtcNow,
            Status = 0,
            ToAccountId = null
        };

        var account = new Account { Id = "acc123", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _transactionService.CreateAsync(userId, request));
    }

    [Fact]
    public async Task CreateAsync_WithTransfer_InvalidDestination_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateTransactionRequestDto
        {
            AccountId = "acc123",
            ToAccountId = "invalid",
            Type = 2, // Transfer
            Amount = 100m,
            Date = DateTime.UtcNow,
            Status = 0
        };

        var account = new Account { Id = "acc123", UserId = userId, Balance = 1000m };
        _accountRepoMock.GetByIdAsync("acc123").Returns(account);
        _accountRepoMock.GetByIdAsync("invalid").Returns((Account?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _transactionService.CreateAsync(userId, request));
    }
}
