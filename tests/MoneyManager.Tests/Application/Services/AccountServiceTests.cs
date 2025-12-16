using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Enums;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class AccountServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IAccountService _accountService;

    public AccountServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _accountService = new AccountService(_unitOfWorkMock);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateAccount()
    {
        // Arrange
        var userId = "user123";
        var request = new CreateAccountRequestDto
        {
            Name = "Checking Account",
            Type = 0,
            InitialBalance = 1000m
        };

        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository.AddAsync(Arg.Any<Account>()).Returns(x => x.Arg<Account>());

        _unitOfWorkMock.Accounts.Returns(accountRepository);

        // Act
        var result = await _accountService.CreateAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.InitialBalance, result.Balance);
        await accountRepository.Received(1).AddAsync(Arg.Any<Account>());
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnUserAccounts()
    {
        // Arrange
        var userId = "user123";
        var accounts = new List<Account>
        {
            new Account { Id = "1", UserId = userId, Name = "Checking", Type = AccountType.Checking, Balance = 1000 },
            new Account { Id = "2", UserId = userId, Name = "Savings", Type = AccountType.Savings, Balance = 5000 }
        };

        var accountRepository = Substitute.For<IRepository<Account>>();
        accountRepository.GetAllAsync().Returns(accounts);

        _unitOfWorkMock.Accounts.Returns(accountRepository);

        // Act
        var result = await _accountService.GetAllAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
}
