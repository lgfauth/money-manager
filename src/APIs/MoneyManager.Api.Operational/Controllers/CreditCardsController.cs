using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/credit-cards")]
[Authorize]
public class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;
    private readonly ICreditCardInvoiceService _invoiceService;
    private readonly IValidator<CreateCreditCardRequestDto> _validator;
    private readonly IValidator<PayCreditCardInvoiceRequestDto> _payValidator;

    public CreditCardsController(
        ICreditCardService creditCardService,
        ICreditCardInvoiceService invoiceService,
        IValidator<CreateCreditCardRequestDto> validator,
        IValidator<PayCreditCardInvoiceRequestDto> payValidator)
    {
        _creditCardService = creditCardService;
        _invoiceService = invoiceService;
        _validator = validator;
        _payValidator = payValidator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCreditCardRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _creditCardService.CreateAsync(HttpContext.GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
            var result = await _creditCardService.GetAllAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var result = await _creditCardService.GetByIdAsync(HttpContext.GetUserId(), id);
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

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CreateCreditCardRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _creditCardService.UpdateAsync(HttpContext.GetUserId(), id, request);
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
            await _creditCardService.DeleteAsync(HttpContext.GetUserId(), id);
            return NoContent();
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

    [HttpGet("{id}/invoices")]
    public async Task<IActionResult> GetInvoices(string id)
    {
        try
        {
            var result = await _invoiceService.GetByCardAsync(HttpContext.GetUserId(), id);
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

    [HttpGet("{id}/invoices/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceDetail(string id, string invoiceId)
    {
        try
        {
            var result = await _invoiceService.GetDetailAsync(HttpContext.GetUserId(), invoiceId);
            if (result.Invoice.CreditCardId != id)
                return this.ApiNotFound();
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

    [HttpPost("{id}/invoices/{invoiceId}/pay")]
    public async Task<IActionResult> PayInvoice(string id, string invoiceId, [FromBody] PayCreditCardInvoiceRequestDto request)
    {
        var validation = await _payValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _invoiceService.PayAsync(HttpContext.GetUserId(), invoiceId, request);
            if (result.CreditCardId != id)
                return this.ApiNotFound();
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
}
