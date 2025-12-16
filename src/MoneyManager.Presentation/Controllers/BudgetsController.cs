using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using System.Security.Claims;

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

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetRequestDto request)
    {
        try
        {
            var result = await _budgetService.CreateOrUpdateAsync(GetUserId(), request.Month, request.Items);
            return CreatedAtAction(nameof(GetByMonth), new { month = request.Month }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{month}")]
    public async Task<IActionResult> GetByMonth(string month)
    {
        try
        {
            var result = await _budgetService.GetByMonthAsync(GetUserId(), month);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _budgetService.GetAllAsync(GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{month}")]
    public async Task<IActionResult> Update(string month, [FromBody] CreateBudgetRequestDto request)
    {
        try
        {
            var result = await _budgetService.CreateOrUpdateAsync(GetUserId(), month, request.Items);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
