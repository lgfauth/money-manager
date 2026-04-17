using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/credit-card-transactions")]
[Authorize]
public class CreditCardTransactionsController : ControllerBase
{
    private readonly ICreditCardTransactionService _service;
    private readonly IValidator<CreateCreditCardTransactionRequestDto> _validator;

    public CreditCardTransactionsController(
        ICreditCardTransactionService service,
        IValidator<CreateCreditCardTransactionRequestDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardTransactionRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _service.CreateAsync(HttpContext.GetUserId(), request);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? creditCardId = null)
    {
        try
        {
            var userId = HttpContext.GetUserId();
            var result = string.IsNullOrWhiteSpace(creditCardId)
                ? await _service.GetAllAsync(userId)
                : await _service.GetByCardAsync(userId, creditCardId);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _service.DeleteAsync(HttpContext.GetUserId(), id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }
}
