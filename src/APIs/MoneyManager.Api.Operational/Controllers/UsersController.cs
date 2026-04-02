using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;

    public UsersController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpPost("accept-terms")]
    public async Task<IActionResult> AcceptTerms([FromBody] AcceptTermsRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        var profile = await _userProfileService.AcceptTermsAsync(userId, request);
        return Ok(profile);
    }
}
