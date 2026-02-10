using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using FluentValidation;
using System.Security.Claims;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IValidator<CreateTransactionRequestDto> _validator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        IValidator<CreateTransactionRequestDto> validator,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _validator = validator;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequestDto request)
    {
        var userId = GetUserId();
        _logger.LogInformation("Creating transaction for user {UserId}, type: {Type}, amount: {Amount}",
            userId, request.Type, request.Amount);

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Transaction validation failed for user {UserId}: {Errors}",
                userId, string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validation.Errors);
        }

        try
        {
            var result = await _transactionService.CreateAsync(userId, request);
            _logger.LogInformation("Transaction {TransactionId} created successfully for user {UserId}",
                result.Id, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        _logger.LogDebug("Fetching all transactions for user {UserId}", userId);

        try
        {
            var result = await _transactionService.GetAllAsync(userId);
            _logger.LogDebug("Retrieved {Count} transactions for user {UserId}",
                result.Count(), userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching transactions for user {UserId}", userId);
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var result = await _transactionService.GetByIdAsync(GetUserId(), id);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateTransactionRequestDto request)
    {
        var userId = GetUserId();
        _logger.LogInformation("Updating transaction {TransactionId} for user {UserId}",
            id, userId);

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(validation.Errors);

        try
        {
            var result = await _transactionService.UpdateAsync(userId, id, request);
            _logger.LogInformation("Transaction {TransactionId} updated successfully for user {UserId}",
                id, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Transaction {TransactionId} not found for user {UserId}",
                id, userId);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating transaction {TransactionId} for user {UserId}",
                id, userId);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = GetUserId();
        _logger.LogInformation("Deleting transaction {TransactionId} for user {UserId}",
            id, userId);

        try
        {
            await _transactionService.DeleteAsync(userId, id);
            _logger.LogInformation("Transaction {TransactionId} deleted successfully for user {UserId}",
                id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Transaction {TransactionId} not found for user {UserId}",
                id, userId);
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting transaction {TransactionId} for user {UserId}",
                id, userId);
            return BadRequest(ex.Message);
        }
    }
}
