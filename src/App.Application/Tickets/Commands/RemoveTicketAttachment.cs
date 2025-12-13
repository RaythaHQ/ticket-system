using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class RemoveTicketAttachment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid AttachmentId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.AttachmentId)
                .NotEmpty()
                .WithMessage("Attachment ID is required.");

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    var attachment = await db.TicketAttachments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == request.AttachmentId.Guid, cancellationToken);

                    if (attachment == null)
                    {
                        context.AddFailure("AttachmentId", "Attachment not found.");
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
            var attachment = await _db.TicketAttachments
                .FirstAsync(a => a.Id == request.AttachmentId.Guid, cancellationToken);

            var ticketId = attachment.TicketId;
            var displayName = attachment.DisplayName;

            _db.TicketAttachments.Remove(attachment);

            // Add change log entry
            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticketId,
                ActorStaffId = _currentUser.UserId?.Guid,
                Message = $"Removed file \"{displayName}\""
            };
            _db.TicketChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.AttachmentId);
        }
    }
}

