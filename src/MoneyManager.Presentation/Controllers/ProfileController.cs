using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public ProfileController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = HttpContext.GetUserId();
        var profile = await _userProfileService.GetProfileAsync(userId);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        var profile = await _userProfileService.UpdateProfileAsync(userId, request);
        return Ok(profile);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        await _userProfileService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Password changed successfully" });
    }

    [HttpPost("update-email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        var profile = await _userProfileService.UpdateEmailAsync(userId, request);
        return Ok(profile);
    }
}
