using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MoneyManager.Infrastructure.Data;

namespace MoneyManager.Infrastructure.WorkerControl;

public sealed class WorkerCommandQueueService
{
    private readonly IMongoCollection<WorkerCommandDocument> _collection;
    private readonly IMongoCollection<WorkerControlStateDocument> _controlStates;
    private readonly IMongoCollection<WorkerScheduleStateDocument> _scheduleStates;

    public WorkerCommandQueueService(MongoContext mongoContext)
    {
        _collection = mongoContext.GetCollection<WorkerCommandDocument>("worker_commands");
        _controlStates = mongoContext.GetCollection<WorkerControlStateDocument>("worker_control_states");
        _scheduleStates = mongoContext.GetCollection<WorkerScheduleStateDocument>("worker_schedule_states");
    }

    public async Task<EnqueueWorkerCommandResult> EnqueueRunNowAsync(string jobName, string requestedBy, string? reason)
        => await EnqueueCommandAsync(jobName, "run-now", requestedBy, reason);

    public async Task<EnqueueWorkerCommandResult> EnqueuePauseAsync(string jobName, string requestedBy, string? reason)
        => await EnqueueCommandAsync(jobName, "pause", requestedBy, reason);

    public async Task<EnqueueWorkerCommandResult> EnqueueResumeAsync(string jobName, string requestedBy, string? reason)
        => await EnqueueCommandAsync(jobName, "resume", requestedBy, reason);

    public async Task<WorkerCommandClaimResult?> ClaimNextCommandAsync(string jobName, string workerName)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<WorkerCommandDocument>.Filter.Eq(x => x.JobName, jobName)
            & Builders<WorkerCommandDocument>.Filter.Eq(x => x.Status, "pending");

        var update = Builders<WorkerCommandDocument>.Update
            .Set(x => x.Status, "processing")
            .Set(x => x.ClaimedBy, workerName)
            .Set(x => x.ClaimedAtUtc, now)
            .Set(x => x.UpdatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<WorkerCommandDocument>
        {
            Sort = Builders<WorkerCommandDocument>.Sort.Ascending(x => x.RequestedAtUtc),
            ReturnDocument = ReturnDocument.After
        };

        var claimed = await _collection.FindOneAndUpdateAsync(filter, update, options);
        if (claimed is null)
        {
            return null;
        }

        return new WorkerCommandClaimResult
        {
            CommandId = claimed.Id,
            JobName = claimed.JobName,
            CommandType = claimed.CommandType,
            RequestedBy = claimed.RequestedBy,
            Reason = claimed.Reason,
            RequestedAtUtc = claimed.RequestedAtUtc,
            ClaimedAtUtc = claimed.ClaimedAtUtc,
            ClaimedBy = claimed.ClaimedBy
        };
    }

    public async Task<JobPauseState> SetPausedStateAsync(string jobName, bool isPaused, string changedBy)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<WorkerControlStateDocument>.Filter.Eq(x => x.JobName, jobName);
        var update = Builders<WorkerControlStateDocument>.Update
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString("N"))
            .Set(x => x.JobName, jobName)
            .Set(x => x.IsPaused, isPaused)
            .Set(x => x.LastChangedAtUtc, now)
            .Set(x => x.LastChangedBy, changedBy)
            .SetOnInsert(x => x.CreatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<WorkerControlStateDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _controlStates.FindOneAndUpdateAsync(filter, update, options);
        return new JobPauseState
        {
            JobName = result.JobName,
            IsPaused = result.IsPaused,
            LastChangedAtUtc = result.LastChangedAtUtc,
            LastChangedBy = result.LastChangedBy
        };
    }

    public async Task<JobPauseState> GetPauseStateAsync(string jobName)
    {
        var state = await _controlStates.Find(x => x.JobName == jobName).FirstOrDefaultAsync();
        if (state is null)
        {
            return new JobPauseState
            {
                JobName = jobName,
                IsPaused = false
            };
        }

        return new JobPauseState
        {
            JobName = state.JobName,
            IsPaused = state.IsPaused,
            LastChangedAtUtc = state.LastChangedAtUtc,
            LastChangedBy = state.LastChangedBy
        };
    }

    public async Task<JobScheduleState> SetScheduleStateAsync(string jobName, ScheduleUpdateRequest request, string changedBy)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<WorkerScheduleStateDocument>.Filter.Eq(x => x.JobName, jobName);
        var update = Builders<WorkerScheduleStateDocument>.Update
            .SetOnInsert(x => x.Id, Guid.NewGuid().ToString("N"))
            .Set(x => x.JobName, jobName)
            .Set(x => x.TimeZoneId, request.TimeZoneId)
            .Set(x => x.Hour, request.Hour)
            .Set(x => x.Minute, request.Minute)
            .Set(x => x.LoopDelaySeconds, request.LoopDelaySeconds)
            .Set(x => x.LastChangedAtUtc, now)
            .Set(x => x.LastChangedBy, changedBy)
            .SetOnInsert(x => x.CreatedAtUtc, now);

        var options = new FindOneAndUpdateOptions<WorkerScheduleStateDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var result = await _scheduleStates.FindOneAndUpdateAsync(filter, update, options);

        return new JobScheduleState
        {
            JobName = result.JobName,
            TimeZoneId = result.TimeZoneId,
            Hour = result.Hour,
            Minute = result.Minute,
            LoopDelaySeconds = result.LoopDelaySeconds,
            LastChangedAtUtc = result.LastChangedAtUtc,
            LastChangedBy = result.LastChangedBy
        };
    }

    public async Task<JobScheduleState?> GetScheduleStateAsync(string jobName)
    {
        var state = await _scheduleStates.Find(x => x.JobName == jobName).FirstOrDefaultAsync();
        if (state is null)
        {
            return null;
        }

        return new JobScheduleState
        {
            JobName = state.JobName,
            TimeZoneId = state.TimeZoneId,
            Hour = state.Hour,
            Minute = state.Minute,
            LoopDelaySeconds = state.LoopDelaySeconds,
            LastChangedAtUtc = state.LastChangedAtUtc,
            LastChangedBy = state.LastChangedBy
        };
    }

    public async Task CompleteAsync(string commandId, bool success, string? errorMessage)
    {
        var now = DateTime.UtcNow;
        var update = Builders<WorkerCommandDocument>.Update
            .Set(x => x.Status, success ? "completed" : "failed")
            .Set(x => x.CompletedAtUtc, now)
            .Set(x => x.ErrorMessage, errorMessage)
            .Set(x => x.UpdatedAtUtc, now);

        await _collection.UpdateOneAsync(x => x.Id == commandId, update);
    }

    private async Task<EnqueueWorkerCommandResult> EnqueueCommandAsync(string jobName, string commandType, string requestedBy, string? reason)
    {
        var existing = await _collection
            .Find(x => x.JobName == jobName
                && x.CommandType == commandType
                && (x.Status == "pending" || x.Status == "processing"))
            .SortByDescending(x => x.RequestedAtUtc)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            return new EnqueueWorkerCommandResult
            {
                CommandId = existing.Id,
                JobName = existing.JobName,
                CommandType = existing.CommandType,
                Status = existing.Status,
                RequestedAtUtc = existing.RequestedAtUtc,
                AlreadyQueued = true
            };
        }

        var now = DateTime.UtcNow;
        var command = new WorkerCommandDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            JobName = jobName,
            CommandType = commandType,
            Status = "pending",
            RequestedBy = requestedBy,
            Reason = reason,
            RequestedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _collection.InsertOneAsync(command);

        return new EnqueueWorkerCommandResult
        {
            CommandId = command.Id,
            JobName = command.JobName,
            CommandType = command.CommandType,
            Status = command.Status,
            RequestedAtUtc = command.RequestedAtUtc,
            AlreadyQueued = false
        };
    }
}

public sealed class EnqueueWorkerCommandResult
{
    public string CommandId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public bool AlreadyQueued { get; set; }
}

public sealed class WorkerCommandClaimResult
{
    public string CommandId { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
    public string? ClaimedBy { get; set; }
}

public sealed class JobPauseState
{
    public string JobName { get; set; } = string.Empty;
    public bool IsPaused { get; set; }
    public DateTime? LastChangedAtUtc { get; set; }
    public string? LastChangedBy { get; set; }
}

public sealed class ScheduleUpdateRequest
{
    public string? TimeZoneId { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int LoopDelaySeconds { get; set; }
}

public sealed class JobScheduleState
{
    public string JobName { get; set; } = string.Empty;
    public string? TimeZoneId { get; set; }
    public int Hour { get; set; }
    public int Minute { get; set; }
    public int LoopDelaySeconds { get; set; }
    public DateTime? LastChangedAtUtc { get; set; }
    public string? LastChangedBy { get; set; }
}

internal sealed class WorkerCommandDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string JobName { get; set; } = string.Empty;

    public string CommandType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string RequestedBy { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? ClaimedAtUtc { get; set; }

    public string? ClaimedBy { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}

internal sealed class WorkerControlStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string JobName { get; set; } = string.Empty;

    public bool IsPaused { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastChangedAtUtc { get; set; }

    public string LastChangedBy { get; set; } = string.Empty;
}

internal sealed class WorkerScheduleStateDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string JobName { get; set; } = string.Empty;

    public string? TimeZoneId { get; set; }

    public int Hour { get; set; }

    public int Minute { get; set; }

    public int LoopDelaySeconds { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastChangedAtUtc { get; set; }

    public string LastChangedBy { get; set; } = string.Empty;
}