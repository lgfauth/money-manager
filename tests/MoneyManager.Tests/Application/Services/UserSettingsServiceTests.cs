using NSubstitute;
using Xunit;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Domain.Interfaces;

namespace MoneyManager.Tests.Application.Services;

public class UserSettingsServiceTests
{
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IUserSettingsService _userSettingsService;

    public UserSettingsServiceTests()
    {
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _userSettingsService = new UserSettingsService(_unitOfWorkMock);
    }

    [Fact]
    public async Task GetSettingsAsync_WithExistingSettings_ShouldReturnSettings()
    {
        // Arrange
        var userId = "user123";
        var settings = new UserSettings
        {
            Id = "settings123",
            UserId = userId,
            Currency = "BRL",
            DateFormat = "dd/MM/yyyy",
            Theme = "dark"
        };

        var settingsList = new List<UserSettings> { settings };
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(settingsList);
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.GetSettingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("BRL", result.Currency);
    }

    [Fact]
    public async Task GetSettingsAsync_WithoutSettings_ShouldCreateDefault()
    {
        // Arrange
        var userId = "user123";
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(new List<UserSettings>());
        settingsRepo.AddAsync(Arg.Any<UserSettings>()).Returns(x => x.Arg<UserSettings>());
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.GetSettingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("BRL", result.Currency);
        await settingsRepo.Received(1).AddAsync(Arg.Any<UserSettings>());
    }

    [Fact]
    public async Task GetOrCreateSettingsAsync_ShouldCreateDefaultSettings()
    {
        // Arrange
        var userId = "user123";
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(new List<UserSettings>());
        settingsRepo.AddAsync(Arg.Any<UserSettings>()).Returns(x => x.Arg<UserSettings>());
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.GetOrCreateSettingsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BRL", result.Currency);
        Assert.Equal("dd/MM/yyyy", result.DateFormat);
        Assert.Equal(1, result.MonthClosingDay);
        Assert.True(result.EmailNotifications);
        Assert.Equal(80, result.BudgetAlertThreshold);
        Assert.Equal("auto", result.Theme);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldUpdateAllSettings()
    {
        // Arrange
        var userId = "user123";
        var existingSettings = new UserSettings
        {
            Id = "settings123",
            UserId = userId,
            Currency = "BRL",
            DateFormat = "dd/MM/yyyy"
        };

        var updatedSettings = new UserSettings
        {
            Currency = "USD",
            DateFormat = "MM/dd/yyyy",
            MonthClosingDay = 5,
            DefaultBudget = 5000m,
            EmailNotifications = false,
            NotifyRecurringProcessed = false,
            NotifyBudgetAlert = false,
            BudgetAlertThreshold = 90,
            NotifyCreditLimitAlert = false,
            CreditLimitAlertThreshold = 85,
            MonthlySummaryEmail = false,
            Theme = "light",
            PrimaryColor = "#ff0000"
        };

        var settingsList = new List<UserSettings> { existingSettings };
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(settingsList);
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.UpdateSettingsAsync(userId, updatedSettings);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("MM/dd/yyyy", result.DateFormat);
        Assert.Equal(5, result.MonthClosingDay);
        Assert.Equal(5000m, result.DefaultBudget);
        Assert.False(result.EmailNotifications);
        Assert.Equal("light", result.Theme);
        await settingsRepo.Received(1).UpdateAsync(Arg.Any<UserSettings>());
    }

    [Fact]
    public async Task UpdateSettingsAsync_ShouldPreserveUserId()
    {
        // Arrange
        var userId = "user123";
        var existingSettings = new UserSettings
        {
            Id = "settings123",
            UserId = userId,
            Currency = "BRL"
        };

        var updatedSettings = new UserSettings
        {
            UserId = "differentUser", // This should not override
            Currency = "USD"
        };

        var settingsList = new List<UserSettings> { existingSettings };
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(settingsList);
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.UpdateSettingsAsync(userId, updatedSettings);

        // Assert
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetSettingsAsync_WithMultipleUsers_ShouldReturnCorrectSettings()
    {
        // Arrange
        var userId = "user123";
        var settings1 = new UserSettings
        {
            UserId = userId,
            Currency = "BRL"
        };
        var settings2 = new UserSettings
        {
            UserId = "other",
            Currency = "USD"
        };

        var settingsList = new List<UserSettings> { settings1, settings2 };
        var settingsRepo = Substitute.For<IRepository<UserSettings>>();
        settingsRepo.GetAllAsync().Returns(settingsList);
        _unitOfWorkMock.UserSettings.Returns(settingsRepo);

        // Act
        var result = await _userSettingsService.GetSettingsAsync(userId);

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal("BRL", result.Currency);
    }
}
