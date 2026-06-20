using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;
using MoneyManager.Observability;

namespace MoneyManager.Tests.Application.Services;

public class AuthServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly ITokenService _tokenServiceMock;
    private readonly ISubscriptionService _subscriptionServiceMock;
    private readonly IProcessLogger _processLoggerMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _tokenServiceMock = Substitute.For<ITokenService>();
        _subscriptionServiceMock = Substitute.For<ISubscriptionService>();
        _processLoggerMock = Substitute.For<IProcessLogger>();
        _authService = new AuthService(_unitOfWorkMock, _tokenServiceMock, _subscriptionServiceMock, _processLoggerMock);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ShouldCreateUser()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Password123"
        };

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync(request.Email).Returns((User?)null);
        userRepository.AddAsync(Arg.Any<User>()).Returns(x => x.Arg<User>());

        _unitOfWorkMock.Users.Returns(userRepository);

        _subscriptionServiceMock.ActivateTrialAsync(Arg.Any<string>())
            .Returns(new SubscriptionResponseDto());

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Email, result.Email);
        await userRepository.Received(1).AddAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "existing@example.com",
            Password = "Password123"
        };

        var existingUser = new User { Id = "1", Email = request.Email };
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync(request.Email).Returns(existingUser);

        _unitOfWorkMock.Users.Returns(userRepository);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "john@example.com",
            Password = "Password123"
        };

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            Id = "1",
            Name = "John Doe",
            Email = request.Email,
            PasswordHash = passwordHash
        };

        var subscriptionRepository = Substitute.For<ISubscriptionRepository>();
        subscriptionRepository.GetByUserIdAsync(user.Id).Returns((Subscription?)null);

        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByEmailAsync(request.Email).Returns(user);

        _unitOfWorkMock.Users.Returns(userRepository);
        _unitOfWorkMock.Subscriptions.Returns(subscriptionRepository);
        _tokenServiceMock.GenerateToken(user.Id, user.Email, user.Name, Arg.Any<IEnumerable<System.Security.Claims.Claim>>()).Returns("token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Name, result.Name);
        Assert.Equal("token", result.Token);
    }
}
