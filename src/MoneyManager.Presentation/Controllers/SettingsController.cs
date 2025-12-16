using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Entities;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IUserSettingsService _userSettingsService;

    public SettingsController(IUserSettingsService userSettingsService)
    {
        _userSettingsService = userSettingsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var userId = HttpContext.GetUserId();
        var settings = await _userSettingsService.GetSettingsAsync(userId);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UserSettings settings)
    {
        var userId = HttpContext.GetUserId();
        var updatedSettings = await _userSettingsService.UpdateSettingsAsync(userId, settings);
        return Ok(updatedSettings);
    }
}
