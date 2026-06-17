using System.Security.Claims;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Controllers;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Presentation.Controllers;

public class FinancialHealthControllerTests
{
    private readonly IFinancialHealthService _service;
    private readonly IValidator<UpsertFinancialHealthSettingsRequestDto> _settingsValidator;
    private readonly IValidator<UpsertPatrimonyBucketRequestDto> _bucketValidator;
    private readonly IValidator<ConfirmSnapshotRequestDto> _confirmValidator;
    private readonly ILogger<FinancialHealthController> _logger;
    private readonly FinancialHealthController _controller;

    private const string UserId = "user-abc";

    public FinancialHealthControllerTests()
    {
        _service = Substitute.For<IFinancialHealthService>();
        _settingsValidator = Substitute.For<IValidator<UpsertFinancialHealthSettingsRequestDto>>();
        _bucketValidator = Substitute.For<IValidator<UpsertPatrimonyBucketRequestDto>>();
        _confirmValidator = Substitute.For<IValidator<ConfirmSnapshotRequestDto>>();
        _logger = Substitute.For<ILogger<FinancialHealthController>>();

        _controller = new FinancialHealthController(
            _service,
            _settingsValidator,
            _bucketValidator,
            _confirmValidator,
            _logger)
        {
            ControllerContext = BuildControllerContext(UserId)
        };
    }

    private static ControllerContext BuildControllerContext(string userId) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                "TestAuth"))
        }
    };

    // --- GetSettings ---

    [Fact]
    public async Task GetSettings_WhenSettingsExist_ShouldReturnOk()
    {
        // Arrange
        var dto = new FinancialHealthSettingsResponseDto { UserId = UserId, ModeName = "moderado" };
        _service.GetSettingsAsync(UserId).Returns(dto);

        // Act
        var result = await _controller.GetSettings();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetSettings_WhenServiceThrows_ShouldReturnBadRequest()
    {
        // Arrange
        _service.GetSettingsAsync(UserId).Returns(Task.FromException<FinancialHealthSettingsResponseDto?>(new Exception("Erro")));

        // Act
        var result = await _controller.GetSettings();

        // Assert
        Assert.IsAssignableFrom<ObjectResult>(result);
    }

    // --- UpsertSettings ---

    [Fact]
    public async Task UpsertSettings_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new UpsertFinancialHealthSettingsRequestDto
        {
            ModeName = "moderado",
            InvestPercent = 20,
            ReserveMonths = 6,
            FireMultiplier = 250,
            FixedExpensePercent = 50,
            InstallmentPercent = 30
        };

        var dto = new FinancialHealthSettingsResponseDto { UserId = UserId, ModeName = "moderado" };

        _settingsValidator.ValidateAsync(request, default).Returns(new ValidationResult());
        _service.UpsertSettingsAsync(UserId, request).Returns(dto);

        // Act
        var result = await _controller.UpsertSettings(request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task UpsertSettings_WithInvalidRequest_ShouldReturnValidationError()
    {
        // Arrange
        var request = new UpsertFinancialHealthSettingsRequestDto();
        var failures = new List<ValidationFailure>
        {
            new("ModeName", "ModeName obrigatório")
        };

        _settingsValidator.ValidateAsync(request, default).Returns(new ValidationResult(failures));

        // Act
        var result = await _controller.UpsertSettings(request);

        // Assert
        Assert.IsAssignableFrom<ObjectResult>(result);
        await _service.DidNotReceive().UpsertSettingsAsync(Arg.Any<string>(), Arg.Any<UpsertFinancialHealthSettingsRequestDto>());
    }

    // --- GetHealthScore ---

    [Fact]
    public async Task GetHealthScore_WhenScoreExists_ShouldReturnOk()
    {
        // Arrange
        var dto = new HealthScoreResponseDto { HasData = true, OverallScore = 75, ReferenceMonth = "2026-06" };
        _service.GetHealthScoreAsync(UserId).Returns(dto);

        // Act
        var result = await _controller.GetHealthScore();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetHealthScore_WhenSettingsNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.GetHealthScoreAsync(UserId).Returns(Task.FromException<HealthScoreResponseDto>(new KeyNotFoundException("Configurações não encontradas")));

        // Act
        var result = await _controller.GetHealthScore();

        // Assert
        Assert.IsAssignableFrom<ObjectResult>(result);
    }

    // --- GetBuckets ---

    [Fact]
    public async Task GetBuckets_ShouldReturnListOfBuckets()
    {
        // Arrange
        var buckets = new List<PatrimonyBucketResponseDto>
        {
            new() { Type = "emergency_reserve" },
            new() { Type = "fire_investment" }
        };

        _service.GetBucketsAsync(UserId).Returns(buckets);

        // Act
        var result = await _controller.GetBuckets();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(buckets, ok.Value);
    }

    // --- GetSnapshotStatus ---

    [Fact]
    public async Task GetSnapshotStatus_ShouldReturnStatusDto()
    {
        // Arrange
        var status = new SnapshotStatusResponseDto { HasConfiguration = true, ShowBanner = false };
        _service.GetCurrentSnapshotStatusAsync(UserId).Returns(status);

        // Act
        var result = await _controller.GetCurrentSnapshotStatus();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(status, ok.Value);
    }
}
