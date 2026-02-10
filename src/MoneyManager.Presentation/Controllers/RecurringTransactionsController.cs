using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecurringTransactionsController : ControllerBase
{
    private readonly IRecurringTransactionService _recurringTransactionService;
    private readonly ILogger<RecurringTransactionsController> _logger;

    public RecurringTransactionsController(
        IRecurringTransactionService recurringTransactionService,
        ILogger<RecurringTransactionsController> logger)
    {
        _recurringTransactionService = recurringTransactionService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecurringTransactionRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        var result = await _recurringTransactionService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = HttpContext.GetUserId();
        var result = await _recurringTransactionService.GetAllAsync(userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var userId = HttpContext.GetUserId();
        var result = await _recurringTransactionService.GetByIdAsync(userId, id);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateRecurringTransactionRequestDto request)
    {
        var userId = HttpContext.GetUserId();
        var result = await _recurringTransactionService.UpdateAsync(userId, id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = HttpContext.GetUserId();
        await _recurringTransactionService.DeleteAsync(userId, id);
        return NoContent();
    }
}
