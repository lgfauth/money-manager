using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Application.Validators;
using MoneyManager.Presentation.Controllers;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Presentation.Controllers;

public class ProfileControllerTests
{
    private const string UserId = "user-123";

    private readonly IUserProfileService _profileService = Substitute.For<IUserProfileService>();
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _controller = new ProfileController(
            _profileService,
            new ChangePasswordRequestValidator(),
            new UpdateEmailRequestValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, UserId) },
                        "TestAuth"))
                }
            }
        };
    }

    [Fact]
    public async Task ChangePassword_WithFrontendPayload_ShouldCallService()
    {
        // Arrange: mesmo JSON enviado pelo formulário de perfil do frontend
        const string payload = """
            {
                "currentPassword": "SenhaAtual@2026",
                "newPassword": "NovaSenha@2026",
                "confirmPassword": "NovaSenha@2026"
            }
            """;
        var request = JsonSerializer.Deserialize<ChangePasswordRequestDto>(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        await _profileService.Received(1).ChangePasswordAsync(UserId, Arg.Any<ChangePasswordRequestDto>());
    }

    [Fact]
    public async Task ChangePassword_WithWeakPassword_ShouldReturnBadRequestWithoutCallingService()
    {
        // Arrange
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "SenhaAtual@2026",
            NewPassword = "123456",
            ConfirmPassword = "123456"
        };

        // Act
        var result = await _controller.ChangePassword(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        await _profileService.DidNotReceive().ChangePasswordAsync(Arg.Any<string>(), Arg.Any<ChangePasswordRequestDto>());
    }

    [Fact]
    public async Task UpdateEmail_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateEmailRequestDto { NewEmail = "novo@example.com", Password = "SenhaAtual@2026" };
        _profileService.UpdateEmailAsync(UserId, request)
            .Returns(new UserProfileResponseDto { Id = UserId, Email = request.NewEmail });

        // Act
        var result = await _controller.UpdateEmail(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateEmail_WithInvalidEmail_ShouldReturnBadRequestWithoutCallingService()
    {
        // Arrange
        var request = new UpdateEmailRequestDto { NewEmail = "email-invalido", Password = "SenhaAtual@2026" };

        // Act
        var result = await _controller.UpdateEmail(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        await _profileService.DidNotReceive().UpdateEmailAsync(Arg.Any<string>(), Arg.Any<UpdateEmailRequestDto>());
    }
}
