using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Imports.Commands;

public class CreateImportJob
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        /// <summary>
        /// The type of entity to import (contacts or tickets).
        /// </summary>
        public string EntityType { get; init; } = null!;

        /// <summary>
        /// The import mode (insert_if_not_exists, update_existing_only, upsert).
        /// </summary>
        public string Mode { get; init; } = null!;

        /// <summary>
        /// Whether this is a dry run (validate only, no changes).
        /// </summary>
        public bool IsDryRun { get; init; }

        /// <summary>
        /// The ID of the uploaded CSV file.
        /// </summary>
        public ShortGuid SourceMediaItemId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser, IAppDbContext db)
        {
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        if (!currentUser.UserId.HasValue)
                            return false;

                        var user = await db
                            .Users.Include(u => u.Roles)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(
                                u => u.Id == currentUser.UserId.Value.Guid,
                                cancellationToken
                            );

                        if (user == null)
                            return false;

                        // Check if user has ImportExportTickets permission
                        return user.Roles.Any(r =>
                            r.SystemPermissions.HasFlag(SystemPermissions.ImportExportTickets)
                        );
                    }
                )
                .WithMessage("You do not have permission to import data.");

            RuleFor(x => x.EntityType)
                .NotEmpty()
                .Must(et =>
                    ImportEntityType.SupportedTypes.Any(t => t.DeveloperName == et.ToLower())
                )
                .WithMessage("Invalid entity type. Must be 'contacts' or 'tickets'.");

            RuleFor(x => x.Mode)
                .NotEmpty()
                .Must(m => ImportMode.SupportedTypes.Any(t => t.DeveloperName == m.ToLower()))
                .WithMessage(
                    "Invalid import mode. Must be 'insert_if_not_exists', 'update_existing_only', or 'upsert'."
                );

            RuleFor(x => x.SourceMediaItemId)
                .NotEmpty()
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        var mediaItem = await db
                            .MediaItems.AsNoTracking()
                            .FirstOrDefaultAsync(m => m.Id == id.Guid, cancellationToken);
                        return mediaItem != null;
                    }
                )
                .WithMessage("Source file not found.");
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
            IBackgroundTaskQueue backgroundTaskQueue
        )
        {
            _db = db;
            _currentUser = currentUser;
            _backgroundTaskQueue = backgroundTaskQueue;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var now = DateTime.UtcNow;

            // Create import job
            var importJob = new ImportJob
            {
                Id = Guid.NewGuid(),
                RequesterUserId = _currentUser.UserId!.Value.Guid,
                RequestedAt = now,
                EntityType = ImportEntityType.From(request.EntityType),
                Mode = ImportMode.From(request.Mode),
                IsDryRun = request.IsDryRun,
                SourceMediaItemId = request.SourceMediaItemId.Guid,
                Status = ImportJobStatus.Queued,
                ProgressStage = "Queued",
                ExpiresAt = now.AddHours(72),
                IsCleanedUp = false,
            };

            _db.ImportJobs.Add(importJob);
            await _db.SaveChangesAsync(cancellationToken);

            // Enqueue background task based on entity type
            Guid backgroundTaskId;
            if (importJob.EntityType == ImportEntityType.Contacts)
            {
                backgroundTaskId = await _backgroundTaskQueue.EnqueueAsync<ContactImportJob>(
                    new { ImportJobId = importJob.Id },
                    cancellationToken
                );
            }
            else
            {
                backgroundTaskId = await _backgroundTaskQueue.EnqueueAsync<TicketImportJob>(
                    new { ImportJobId = importJob.Id },
                    cancellationToken
                );
            }

            // Update import job with background task reference
            importJob.BackgroundTaskId = backgroundTaskId;
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(importJob.Id);
        }
    }
}

/// <summary>
/// Base class for the contact import background task. Actual implementation is in Infrastructure layer.
/// Registered in DI so QueuedHostedService can resolve it.
/// </summary>
public abstract class ContactImportJob : IExecuteBackgroundTask
{
    public abstract Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for the ticket import background task. Actual implementation is in Infrastructure layer.
/// Registered in DI so QueuedHostedService can resolve it.
/// </summary>
public abstract class TicketImportJob : IExecuteBackgroundTask
{
    public abstract Task Execute(Guid jobId, JsonElement args, CancellationToken cancellationToken);
}
