using System.Text.Json;
using App.Application.Common.Exceptions;
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

public class RetryExportJob
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid OriginalExportJobId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser, IAppDbContext db)
        {
            RuleFor(x => x.OriginalExportJobId)
                .MustAsync(async (id, cancellationToken) =>
                {
                    var job = await db.ExportJobs.AsNoTracking()
                        .FirstOrDefaultAsync(j => j.Id == id.Guid, cancellationToken);
                    return job != null && job.Status == ExportJobStatus.Failed;
                })
                .WithMessage("Export job not found or is not in a failed state.");

            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (!currentUser.UserId.HasValue) return false;
                    
                    var user = await db.Users
                        .Include(u => u.Roles)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == currentUser.UserId.Value.Guid, cancellationToken);
                    
                    if (user == null) return false;
                    
                    return user.Roles.Any(r => r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets));
                })
                .WithMessage("You do not have permission to retry exports.");
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
            var originalJob = await _db.ExportJobs
                .FirstOrDefaultAsync(j => j.Id == request.OriginalExportJobId.Guid, cancellationToken);

            if (originalJob == null)
                throw new NotFoundException("ExportJob", request.OriginalExportJobId);

            var now = DateTime.UtcNow;

            // Create a new export job with the same snapshot payload
            var newExportJob = new ExportJob
            {
                Id = Guid.NewGuid(),
                RequesterUserId = _currentUser.UserId!.Value.Guid,
                RequestedAt = now,
                Status = ExportJobStatus.Queued,
                ProgressStage = "Queued (Retry)",
                SnapshotPayloadJson = originalJob.SnapshotPayloadJson,
                ExpiresAt = now.AddHours(72),
                IsCleanedUp = false,
            };

            _db.ExportJobs.Add(newExportJob);
            await _db.SaveChangesAsync(cancellationToken);

            // Enqueue background task
            var backgroundTaskId = await _backgroundTaskQueue.EnqueueAsync<TicketExportJob>(
                new { ExportJobId = newExportJob.Id },
                cancellationToken);

            newExportJob.BackgroundTaskId = backgroundTaskId;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(newExportJob.Id);
        }
    }
}

