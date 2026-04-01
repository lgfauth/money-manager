using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Domain.Enums;
using FluentValidation;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IValidator<CreateCategoryRequestDto> _validator;

    public CategoriesController(ICategoryService categoryService, IValidator<CreateCategoryRequestDto> validator)
    {
        _categoryService = categoryService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _categoryService.CreateAsync(HttpContext.GetUserId(), request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? type)
    {
        try
        {
            var result = await _categoryService.GetAllAsync(HttpContext.GetUserId(), type.HasValue ? (CategoryType)type.Value : null);
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
            var result = await _categoryService.GetByIdAsync(HttpContext.GetUserId(), id);
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
    public async Task<IActionResult> Update(string id, [FromBody] CreateCategoryRequestDto request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _categoryService.UpdateAsync(HttpContext.GetUserId(), id, request);
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
            await _categoryService.DeleteAsync(HttpContext.GetUserId(), id);
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
}
