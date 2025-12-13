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

public class CreateExportJob
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ExportSnapshotPayload SnapshotPayload { get; init; } = new();
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
                    
                    // Check if user has ImportExportTickets permission
                    return user.Roles.Any(r => r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets));
                })
                .WithMessage("You do not have permission to export tickets.");

            RuleFor(x => x.SnapshotPayload.Columns)
                .NotEmpty()
                .WithMessage("At least one column must be selected for export.");
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
            
            // Create export job
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
            var backgroundTaskId = await _backgroundTaskQueue.EnqueueAsync<TicketExportJob>(
                new { ExportJobId = exportJob.Id },
                cancellationToken);

            // Update export job with background task reference
            exportJob.BackgroundTaskId = backgroundTaskId;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(exportJob.Id);
        }
    }
}

/// <summary>
/// Base class for the ticket export background task. Actual implementation is in Infrastructure layer.
/// Registered in DI so QueuedHostedService can resolve it.
/// </summary>
public abstract class TicketExportJob : IExecuteBackgroundTask
{
    public abstract Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}

