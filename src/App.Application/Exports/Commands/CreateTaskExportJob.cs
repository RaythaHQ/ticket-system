using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Exports.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exports.Commands;

public class CreateTaskExportJob
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public TaskExportSnapshotPayload SnapshotPayload { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser, IAppDbContext db)
        {
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (!currentUser.UserId.HasValue) return false;

                    var user = await db.Users
                        .Include(u => u.Roles)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value.Guid, cancellationToken);

                    if (user == null) return false;

                    // Check if user has ImportExportTickets permission (same permission for task exports)
                    return user.Roles.Any(r => r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets));
                })
                .WithMessage("You do not have permission to export data.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IBackgroundTaskQueue _backgroundTaskQueue;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            IBackgroundTaskQueue backgroundTaskQueue)
        {
            _db = db;
            _currentUser = currentUser;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var exportJob = new ExportJob
            {
                Id = Guid.NewGuid(),
                RequesterUserId = _currentUser.UserId!.Value.Guid,
                RequestedAt = now,
                Status = ExportJobStatus.Queued,
                ProgressStage = "Queued",
                SnapshotPayloadJson = JsonSerializer.Serialize(request.SnapshotPayload),
                ExpiresAt = now.AddHours(72),
                IsCleanedUp = false,
            };

            _db.ExportJobs.Add(exportJob);
            await _db.SaveChangesAsync(cancellationToken);

            // Enqueue background task
            var backgroundTaskId = await _backgroundTaskQueue.EnqueueAsync<TaskExportJob>(
                new { ExportJobId = exportJob.Id },
                cancellationToken);

            exportJob.BackgroundTaskId = backgroundTaskId;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(exportJob.Id);
        }
    }
}

/// <summary>
/// Base class for the task export background task. Actual implementation is in Infrastructure layer.
/// </summary>
public abstract class TaskExportJob : IExecuteBackgroundTask
{
    public abstract Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}
