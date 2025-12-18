using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = HttpContext.GetUserId();
        var status = await _onboardingService.GetOnboardingStatusAsync(userId);
        return Ok(status);
    }

    [HttpGet("category-suggestions")]
    public async Task<IActionResult> GetCategorySuggestions()
    {
        var suggestions = await _onboardingService.GetCategorySuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOnboarding()
    {
        var userId = HttpContext.GetUserId();
        await _onboardingService.CompleteOnboardingAsync(userId);
        return Ok(new { message = "Onboarding completed successfully" });
    }
}
