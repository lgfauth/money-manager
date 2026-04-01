using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Models;
using MoneyManager.Api.Administration.Services;
using MoneyManager.Infrastructure.WorkerControl;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin/jobs")]
[Authorize(Policy = AdminPolicies.Operator)]
public sealed class AdminJobsController : ControllerBase
{
    private static readonly HashSet<string> SupportedJobs =
    [
        "ScheduledTransactionWorker",
        "InvoiceClosureWorker",
        "DailyReminderWorker"
    ];

    private readonly WorkerCommandQueueService _commandQueueService;
    private readonly AdminAuditService _auditService;
    private readonly ILogger<AdminJobsController> _logger;

    public AdminJobsController(
        WorkerCommandQueueService commandQueueService,
        AdminAuditService auditService,
        ILogger<AdminJobsController> logger)
    {
        _commandQueueService = commandQueueService;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpPost("{jobName}/run-now")]
    public async Task<ActionResult<JobCommandResponse>> RunNow(string jobName, [FromBody] RunNowJobRequest? request)
    {
        if (!HasValidReason(request?.Reason))
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        var normalizedJob = NormalizeJobName(jobName);
        if (normalizedJob is null)
        {
            return NotFound(new { message = $"Job '{jobName}' nao encontrado" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var queued = await _commandQueueService.EnqueueRunNowAsync(normalizedJob, operatorUsername, request?.Reason);

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/run-now",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: true,
                result: queued);

            return Ok(new JobCommandResponse
            {
                CommandId = queued.CommandId,
                JobName = queued.JobName,
                CommandType = queued.CommandType,
                Status = queued.Status,
                RequestedAtUtc = queued.RequestedAtUtc,
                AlreadyQueued = queued.AlreadyQueued
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing run-now command for job {JobName}", normalizedJob);

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/run-now",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Erro ao enfileirar comando run-now", errors = new[] { ex.Message } });
        }
    }

    [HttpPost("{jobName}/pause")]
    public async Task<ActionResult<JobCommandResponse>> Pause(string jobName, [FromBody] RunNowJobRequest? request)
    {
        if (!HasValidReason(request?.Reason))
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        return await EnqueueCommand(jobName, "pause", request);
    }

    [HttpPost("{jobName}/resume")]
    public async Task<ActionResult<JobCommandResponse>> Resume(string jobName, [FromBody] RunNowJobRequest? request)
    {
        if (!HasValidReason(request?.Reason))
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        return await EnqueueCommand(jobName, "resume", request);
    }

    [HttpPut("{jobName}/schedule")]
    [Authorize(Policy = AdminPolicies.Admin)]
    public async Task<ActionResult<JobScheduleResponse>> UpdateSchedule(string jobName, [FromBody] UpdateJobScheduleRequest request)
    {
        if (!HasValidReason(request.Reason))
        {
            return BadRequest(new { message = "reason is required and must have at least 10 characters" });
        }

        var normalizedJob = NormalizeJobName(jobName);
        if (normalizedJob is null)
        {
            return NotFound(new { message = $"Job '{jobName}' nao encontrado" });
        }

        if (request.Hour is < 0 or > 23)
        {
            return BadRequest(new { message = "Hour deve estar entre 0 e 23" });
        }

        if (request.Minute is < 0 or > 59)
        {
            return BadRequest(new { message = "Minute deve estar entre 0 e 59" });
        }

        if (request.LoopDelaySeconds is < 5 or > 3600)
        {
            return BadRequest(new { message = "LoopDelaySeconds deve estar entre 5 e 3600" });
        }

        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId);
            }
            catch
            {
                return BadRequest(new { message = "TimeZoneId invalido para este ambiente" });
            }
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var updated = await _commandQueueService.SetScheduleStateAsync(
                normalizedJob,
                new MoneyManager.Infrastructure.WorkerControl.ScheduleUpdateRequest
                {
                    TimeZoneId = request.TimeZoneId,
                    Hour = request.Hour,
                    Minute = request.Minute,
                    LoopDelaySeconds = request.LoopDelaySeconds
                },
                operatorUsername);

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/schedule",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: true,
                result: updated);

            return Ok(new JobScheduleResponse
            {
                JobName = updated.JobName,
                TimeZoneId = updated.TimeZoneId,
                Hour = updated.Hour,
                Minute = updated.Minute,
                LoopDelaySeconds = updated.LoopDelaySeconds,
                LastChangedAtUtc = updated.LastChangedAtUtc,
                LastChangedBy = updated.LastChangedBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule for job {JobName}", normalizedJob);

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/schedule",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = "Erro ao atualizar schedule do job", errors = new[] { ex.Message } });
        }
    }

    private async Task<ActionResult<JobCommandResponse>> EnqueueCommand(string jobName, string commandType, RunNowJobRequest? request)
    {
        var normalizedJob = NormalizeJobName(jobName);
        if (normalizedJob is null)
        {
            return NotFound(new { message = $"Job '{jobName}' nao encontrado" });
        }

        var operatorUsername = User.Identity?.Name ?? "unknown";

        try
        {
            var queued = commandType switch
            {
                "pause" => await _commandQueueService.EnqueuePauseAsync(normalizedJob, operatorUsername, request?.Reason),
                "resume" => await _commandQueueService.EnqueueResumeAsync(normalizedJob, operatorUsername, request?.Reason),
                _ => await _commandQueueService.EnqueueRunNowAsync(normalizedJob, operatorUsername, request?.Reason)
            };

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/{commandType}",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: true,
                result: queued);

            return Ok(new JobCommandResponse
            {
                CommandId = queued.CommandId,
                JobName = queued.JobName,
                CommandType = queued.CommandType,
                Status = queued.Status,
                RequestedAtUtc = queued.RequestedAtUtc,
                AlreadyQueued = queued.AlreadyQueued
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queueing {CommandType} command for job {JobName}", commandType, normalizedJob);

            await _auditService.RecordAsync(
                action: $"jobs/{normalizedJob}/{commandType}",
                operatorUsername: operatorUsername,
                targetUserId: normalizedJob,
                parameters: request,
                isSuccess: false,
                result: null,
                errorMessage: ex.Message);

            return StatusCode(500, new { message = $"Erro ao enfileirar comando {commandType}", errors = new[] { ex.Message } });
        }
    }

    private static string? NormalizeJobName(string jobName)
    {
        foreach (var supportedJob in SupportedJobs)
        {
            if (string.Equals(supportedJob, jobName, StringComparison.OrdinalIgnoreCase))
            {
                return supportedJob;
            }
        }

        return null;
    }

    private static bool HasValidReason(string? reason)
        => !string.IsNullOrWhiteSpace(reason) && reason.Trim().Length >= 10;
}