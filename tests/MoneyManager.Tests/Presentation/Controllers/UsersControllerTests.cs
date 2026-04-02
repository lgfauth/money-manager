using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Controllers;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Presentation.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task AcceptTerms_WithAuthenticatedUser_ShouldReturnOkWithUpdatedProfile()
    {
        // Arrange
        var userId = "user-123";
        var request = new AcceptTermsRequestDto { TermsVersion = "2026-04" };
        var expected = new UserProfileResponseDto
        {
            Id = userId,
            Name = "User",
            Email = "user@example.com",
            TermsVersion = "2026-04",
            TermsAccepted = true,
            TermsAcceptedAt = DateTime.UtcNow
        };

        var profileService = Substitute.For<IUserProfileService>();
        profileService.AcceptTermsAsync(userId, Arg.Any<AcceptTermsRequestDto>())
            .Returns(expected);

        var controller = new UsersController(profileService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                        "TestAuth"))
                }
            }
        };

        // Act
        var result = await controller.AcceptTerms(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<UserProfileResponseDto>(okResult.Value);
        Assert.Equal(userId, payload.Id);
        Assert.True(payload.TermsAccepted);
        Assert.Equal("2026-04", payload.TermsVersion);

        await profileService.Received(1).AcceptTermsAsync(
            userId,
            Arg.Is<AcceptTermsRequestDto>(x => x.TermsVersion == "2026-04"));
    }
}
