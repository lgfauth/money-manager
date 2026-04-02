using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequestDto request)
    {
        try
        {
            var result = await _budgetService.CreateOrUpdateAsync(HttpContext.GetUserId(), request.Month, request.Items);
            return CreatedAtAction(nameof(GetByMonth), new { month = request.Month }, result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("{month}")]
    public async Task<IActionResult> GetByMonth(string month)
    {
        try
        {
            var result = await _budgetService.GetByMonthAsync(HttpContext.GetUserId(), month);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _budgetService.GetAllAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPut("{month}")]
    public async Task<IActionResult> Update(string month, [FromBody] CreateBudgetRequestDto request)
    {
        try
        {
            var result = await _budgetService.CreateOrUpdateAsync(HttpContext.GetUserId(), month, request.Items);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }
}
