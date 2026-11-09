using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Application.Validators;
using MoneyManager.Domain.Exceptions;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

/// <summary>
/// Importação de fatura fechada de cartão por IA: upload (processado em segundo plano pelo Worker),
/// acompanhamento, cartões elegíveis e descarte.
/// </summary>
[ApiController]
[Route("api/credit-card-invoice-imports")]
[Authorize]
public class CreditCardInvoiceImportsController : ControllerBase
{
    private readonly ICreditCardInvoiceImportService _service;
    private readonly IValidator<CreateInvoiceImportRequestDto> _validator;

    public CreditCardInvoiceImportsController(
        ICreditCardInvoiceImportService service,
        IValidator<CreateInvoiceImportRequestDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(CreateInvoiceImportValidator.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CreateInvoiceImportValidator.MaxRequestBytes)]
    public async Task<IActionResult> Create([FromForm] string? creditCardId, [FromForm] List<IFormFile>? files)
    {
        var request = new CreateInvoiceImportRequestDto { CreditCardId = creditCardId ?? string.Empty };
        foreach (var file in files ?? [])
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            request.Files.Add(new InvoiceImportUploadFileRequestDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType ?? string.Empty,
                Content = ms.ToArray()
            });
        }

        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors, "Arquivos da fatura inválidos.");

        try
        {
            var result = await _service.CreateAsync(HttpContext.GetUserId(), request);
            return Accepted($"/api/credit-card-invoice-imports/{result.Id}", result);
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (InvoiceImportConflictException ex)
        {
            return this.ApiConflict(ex.Message);
        }
    }

    [HttpGet("eligible-cards")]
    public async Task<IActionResult> GetEligibleCards()
    {
        var result = await _service.GetEligibleCardsAsync(HttpContext.GetUserId());
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var result = await _service.GetRecentAsync(HttpContext.GetUserId());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var result = await _service.GetByIdAsync(HttpContext.GetUserId(), id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Discard(string id)
    {
        try
        {
            await _service.DiscardAsync(HttpContext.GetUserId(), id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return this.ApiNotFound(ex.Message);
        }
        catch (InvoiceImportConflictException ex)
        {
            return this.ApiConflict(ex.Message);
        }
    }
}
