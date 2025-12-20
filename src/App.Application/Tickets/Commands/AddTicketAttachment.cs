using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class AddTicketAttachment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long TicketId { get; init; }
        public ShortGuid MediaItemId { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.TicketId)
                .GreaterThan(0)
                .WithMessage("Ticket ID is required.");

            RuleFor(x => x.MediaItemId)
                .NotEmpty()
                .WithMessage("Media item ID is required.");

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    var ticket = await db.Tickets
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

                    if (ticket == null)
                    {
                        context.AddFailure("TicketId", "Ticket not found.");
                        return;
                    }

                    var mediaItem = await db.MediaItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == request.MediaItemId.Guid, cancellationToken);

                    if (mediaItem == null)
                    {
                        context.AddFailure("MediaItemId", "Media item not found.");
                        return;
                    }

                    // Check if this media item is already attached to this ticket
                    var existingAttachment = await db.TicketAttachments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.TicketId == request.TicketId && a.MediaItemId == request.MediaItemId.Guid, cancellationToken);

                    if (existingAttachment != null)
                    {
                        context.AddFailure("MediaItemId", "This file is already attached to the ticket.");
                        return;
                    }
                });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var mediaItem = await _db.MediaItems
                .AsNoTracking()
                .FirstAsync(m => m.Id == request.MediaItemId.Guid, cancellationToken);

            var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? mediaItem.FileName : request.DisplayName;

            var attachment = new TicketAttachment
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                MediaItemId = request.MediaItemId.Guid,
                DisplayName = displayName,
                Description = request.Description,
                UploadedByUserId = _currentUser.UserId!.Value.Guid
            };

            _db.TicketAttachments.Add(attachment);

            // Add change log entry
            var changeLog = new TicketChangeLogEntry
            {
                TicketId = request.TicketId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Attached file \"{displayName}\""
            };
            _db.TicketChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(attachment.Id);
        }
    }
}

