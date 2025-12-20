using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

public class AddContactAttachment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long ContactId { get; init; }
        public ShortGuid MediaItemId { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.ContactId)
                .GreaterThan(0)
                .WithMessage("Contact ID is required.");

            RuleFor(x => x.MediaItemId)
                .NotEmpty()
                .WithMessage("Media item ID is required.");

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    var contact = await db.Contacts
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == request.ContactId, cancellationToken);

                    if (contact == null)
                    {
                        context.AddFailure("ContactId", "Contact not found.");
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

                    // Check if this media item is already attached to this contact
                    var existingAttachment = await db.ContactAttachments
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.ContactId == request.ContactId && a.MediaItemId == request.MediaItemId.Guid, cancellationToken);

                    if (existingAttachment != null)
                    {
                        context.AddFailure("MediaItemId", "This file is already attached to the contact.");
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

            var attachment = new ContactAttachment
            {
                Id = Guid.NewGuid(),
                ContactId = request.ContactId,
                MediaItemId = request.MediaItemId.Guid,
                DisplayName = displayName,
                Description = request.Description,
                UploadedByUserId = _currentUser.UserId!.Value.Guid
            };

            _db.ContactAttachments.Add(attachment);

            // Add change log entry
            var changeLog = new ContactChangeLogEntry
            {
                ContactId = request.ContactId,
                ActorStaffId = _currentUser.UserIdAsGuid,
                Message = $"Attached file \"{displayName}\""
            };
            _db.ContactChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(attachment.Id);
        }
    }
}

