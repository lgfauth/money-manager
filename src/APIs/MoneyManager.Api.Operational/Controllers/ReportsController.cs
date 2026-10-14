using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] string month)
    {
        try
        {
            var result = await _reportService.GetMonthlySummaryAsync(HttpContext.GetUserId(), month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] ReportOverviewRequestDto request)
    {
        try
        {
            var result = await _reportService.GetOverviewAsync(HttpContext.GetUserId(), request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("by-category")]
    public async Task<IActionResult> GetByCategory([FromQuery] string month)
    {
        try
        {
            var result = await _reportService.GetExpensesByCategoryAsync(HttpContext.GetUserId(), month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }
}
