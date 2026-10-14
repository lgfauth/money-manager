using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        try
        {
            var result = await _dashboardService.GetSummaryAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }
}
