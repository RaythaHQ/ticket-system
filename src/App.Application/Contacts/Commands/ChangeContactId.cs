using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

/// <summary>
/// Changes a contact's ID by creating a new record with the specified ID,
/// updating all references (tickets, comments, change log), and hard-deleting the original.
/// </summary>
public class ChangeContactId
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        /// <summary>
        /// The current contact ID to change.
        /// </summary>
        public long CurrentId { get; init; }

        /// <summary>
        /// The new contact ID to assign.
        /// </summary>
        public long NewId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.CurrentId)
                .GreaterThan(0)
                .WithMessage("Current contact ID must be a positive number.");

            RuleFor(x => x.NewId)
                .GreaterThan(0)
                .WithMessage("New contact ID must be a positive number.");

            RuleFor(x => x)
                .Must(x => x.CurrentId != x.NewId)
                .WithMessage("New contact ID must be different from the current ID.");

            RuleFor(x => x.NewId)
                .MustAsync(
                    async (newId, cancellationToken) =>
                    {
                        // Check both active and soft-deleted contacts
                        var exists = await db
                            .Contacts.IgnoreQueryFilters()
                            .AnyAsync(c => c.Id == newId, cancellationToken);
                        return !exists;
                    }
                )
                .WithMessage("A contact with this ID already exists.");

            RuleFor(x => x.CurrentId)
                .MustAsync(
                    async (currentId, cancellationToken) =>
                    {
                        return await db.Contacts.AnyAsync(
                            c => c.Id == currentId,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("The contact to change was not found.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Load the existing contact with all its data
            var existingContact = await _db
                .Contacts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    c => c.Id == request.CurrentId && !c.IsDeleted,
                    cancellationToken
                );

            if (existingContact == null)
                throw new NotFoundException("Contact", request.CurrentId);

            // Double-check new ID doesn't exist (belt and suspenders)
            var newIdExists = await _db
                .Contacts.IgnoreQueryFilters()
                .AnyAsync(c => c.Id == request.NewId, cancellationToken);

            if (newIdExists)
                throw new BusinessException($"A contact with ID {request.NewId} already exists.");

            // Create new contact with the new ID and copy all data
            var newContact = new Contact
            {
                Id = request.NewId,
                FirstName = existingContact.FirstName,
                LastName = existingContact.LastName,
                Email = existingContact.Email,
                PhoneNumbersJson = existingContact.PhoneNumbersJson,
                Address = existingContact.Address,
                OrganizationAccount = existingContact.OrganizationAccount,
                DmeIdentifiersJson = existingContact.DmeIdentifiersJson,
                CreationTime = existingContact.CreationTime,
                CreatorUserId = existingContact.CreatorUserId,
                LastModificationTime = DateTime.UtcNow,
                LastModifierUserId = _currentUser.UserIdAsGuid,
                IsDeleted = false,
            };

            _db.Contacts.Add(newContact);
            await _db.SaveChangesAsync(cancellationToken);

            // Update all related tickets to point to the new contact
            var ticketsToUpdate = await _db
                .Tickets.IgnoreQueryFilters()
                .Where(t => t.ContactId == request.CurrentId)
                .ToListAsync(cancellationToken);

            foreach (var ticket in ticketsToUpdate)
            {
                ticket.ContactId = request.NewId;
            }

            // Update all contact change log entries
            var changeLogEntries = await _db
                .ContactChangeLogEntries.Where(c => c.ContactId == request.CurrentId)
                .ToListAsync(cancellationToken);

            foreach (var entry in changeLogEntries)
            {
                entry.ContactId = request.NewId;
            }

            // Update all contact comments
            var comments = await _db
                .ContactComments.Where(c => c.ContactId == request.CurrentId)
                .ToListAsync(cancellationToken);

            foreach (var comment in comments)
            {
                comment.ContactId = request.NewId;
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Add a changelog entry to the new contact explaining the change
            var changeLogMessage =
                $"Contact ID changed from #{request.CurrentId} to #{request.NewId}";
            var fieldChangesJson = System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    ContactId = new
                    {
                        OldValue = request.CurrentId.ToString(),
                        NewValue = request.NewId.ToString(),
                    },
                }
            );

            var newChangeLogEntry = new ContactChangeLogEntry
            {
                ContactId = request.NewId,
                CreationTime = DateTime.UtcNow,
                ActorStaffId = _currentUser.UserIdAsGuid,
                FieldChangesJson = fieldChangesJson,
                Message = changeLogMessage,
            };

            _db.ContactChangeLogEntries.Add(newChangeLogEntry);

            // Hard delete the old contact record (bypass soft delete by directly removing)
            _db.Contacts.Remove(existingContact);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(request.NewId);
        }
    }
}
