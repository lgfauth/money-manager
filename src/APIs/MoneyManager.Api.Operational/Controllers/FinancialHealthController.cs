using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Application.DTOs.Request;
using MoneyManager.Application.Services;
using MoneyManager.Presentation.Extensions;

namespace MoneyManager.Presentation.Controllers;

[ApiController]
[Route("api/financial-health")]
[Authorize]
public class FinancialHealthController : ControllerBase
{
    private readonly IFinancialHealthService _service;
    private readonly IValidator<UpsertFinancialHealthSettingsRequestDto> _settingsValidator;
    private readonly IValidator<UpsertPatrimonyBucketRequestDto> _bucketValidator;
    private readonly IValidator<ConfirmSnapshotRequestDto> _confirmValidator;
    private readonly ILogger<FinancialHealthController> _logger;

    public FinancialHealthController(
        IFinancialHealthService service,
        IValidator<UpsertFinancialHealthSettingsRequestDto> settingsValidator,
        IValidator<UpsertPatrimonyBucketRequestDto> bucketValidator,
        IValidator<ConfirmSnapshotRequestDto> confirmValidator,
        ILogger<FinancialHealthController> logger)
    {
        _service = service;
        _settingsValidator = settingsValidator;
        _bucketValidator = bucketValidator;
        _confirmValidator = confirmValidator;
        _logger = logger;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var result = await _service.GetSettingsAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpsertSettings([FromBody] UpsertFinancialHealthSettingsRequestDto request)
    {
        var validation = await _settingsValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _service.UpsertSettingsAsync(HttpContext.GetUserId(), request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("buckets")]
    public async Task<IActionResult> GetBuckets()
    {
        try
        {
            var result = await _service.GetBucketsAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPost("buckets")]
    public async Task<IActionResult> UpsertBucket([FromBody] UpsertPatrimonyBucketRequestDto request)
    {
        var validation = await _bucketValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            var result = await _service.UpsertBucketAsync(HttpContext.GetUserId(), request);
            return Ok(result);
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

    [HttpGet("snapshots/current")]
    public async Task<IActionResult> GetCurrentSnapshotStatus()
    {
        try
        {
            var result = await _service.GetCurrentSnapshotStatusAsync(HttpContext.GetUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshotHistory([FromQuery] int year)
    {
        try
        {
            var result = await _service.GetSnapshotHistoryAsync(HttpContext.GetUserId(), year);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPatch("snapshots/{year:int}/{month:int}/confirm")]
    public async Task<IActionResult> ConfirmSnapshot(int year, int month, [FromBody] ConfirmSnapshotRequestDto request)
    {
        var validation = await _confirmValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return this.ApiValidationError(validation.Errors);

        try
        {
            await _service.ConfirmSnapshotAsync(HttpContext.GetUserId(), year, month, request);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return this.ApiNotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return this.ApiNotFound();
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpPatch("snapshots/{year:int}/{month:int}/dismiss")]
    public async Task<IActionResult> DismissSnapshot(int year, int month)
    {
        try
        {
            await _service.DismissSnapshotAsync(HttpContext.GetUserId(), year, month);
            return NoContent();
        }
        catch (Exception ex)
        {
            return this.ApiBadRequest(ex.Message);
        }
    }

    [HttpGet("score")]
    public async Task<IActionResult> GetHealthScore()
    {
        try
        {
            var result = await _service.GetHealthScoreAsync(HttpContext.GetUserId());
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
}
