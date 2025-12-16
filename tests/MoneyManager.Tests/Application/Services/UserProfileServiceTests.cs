using NSubstitute;
using Xunit;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class UserProfileServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserProfileService _userProfileService;

    public UserProfileServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userProfileService = new UserProfileService(_unitOfWorkMock);
    }

    [Fact]
    public async Task GetProfileAsync_WithValidUserId_ShouldReturnProfile()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com",
            FullName = "John Doe Smith",
            Phone = "+5511999999999",
            ProfilePicture = "avatar.jpg"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act
        var result = await _userProfileService.GetProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@example.com", result.Email);
    }

    [Fact]
    public async Task GetProfileAsync_WithInvalidUserId_ShouldThrowException()
    {
        // Arrange
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync("invalid").Returns((User?)null);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _userProfileService.GetProfileAsync("invalid"));
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateUserProfile()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Name = "John Doe",
            Email = "john@example.com"
        };

        var request = new UpdateProfileRequestDto
        {
            FullName = "John Doe Smith",
            Phone = "+5511999999999",
            ProfilePicture = "new-avatar.jpg"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act
        var result = await _userProfileService.UpdateProfileAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe Smith", result.FullName);
        Assert.Equal("+5511999999999", result.Phone);
        await userRepo.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidPassword_ShouldUpdatePassword()
    {
        // Arrange
        var userId = "user123";
        var oldPasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123");
        var user = new User
        {
            Id = userId,
            Email = "john@example.com",
            PasswordHash = oldPasswordHash
        };

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act
        var result = await _userProfileService.ChangePasswordAsync(userId, request);

        // Assert
        Assert.True(result);
        await userRepo.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
        };

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123",
            ConfirmPassword = "NewPassword123"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _userProfileService.ChangePasswordAsync(userId, request));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithMismatchedPasswords_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123")
        };

        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123",
            ConfirmPassword = "DifferentPassword"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _userProfileService.ChangePasswordAsync(userId, request));
    }

    [Fact]
    public async Task UpdateEmailAsync_WithValidEmail_ShouldUpdateEmail()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        };

        var request = new UpdateEmailRequestDto
        {
            NewEmail = "new@example.com",
            Password = "Password123"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        userRepo.GetByEmailAsync("new@example.com").Returns((User?)null);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act
        var result = await _userProfileService.UpdateEmailAsync(userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new@example.com", result.Email);
        await userRepo.Received(1).UpdateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task UpdateEmailAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            Id = userId,
            Email = "old@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123")
        };

        var existingUser = new User
        {
            Id = "other",
            Email = "existing@example.com"
        };

        var request = new UpdateEmailRequestDto
        {
            NewEmail = "existing@example.com",
            Password = "Password123"
        };

        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetByIdAsync(userId).Returns(user);
        userRepo.GetByEmailAsync("existing@example.com").Returns(existingUser);
        _unitOfWorkMock.Users.Returns(userRepo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _userProfileService.UpdateEmailAsync(userId, request));
    }
}
