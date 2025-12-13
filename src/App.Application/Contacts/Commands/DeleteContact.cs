using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Commands;

public class DeleteContact
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var contact = await _db
                .Contacts.Include(c => c.Comments)
                .Include(c => c.ChangeLogEntries)
                .Include(c => c.Tickets)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (contact == null)
                throw new NotFoundException("Contact", request.Id);

            // Check if contact has tickets - don't delete if so
            if (contact.Tickets.Any())
            {
                throw new BusinessException(
                    $"Cannot delete contact with {contact.Tickets.Count} associated ticket(s). Please reassign or delete the tickets first."
                );
            }

            // Remove related entities
            _db.ContactComments.RemoveRange(contact.Comments);
            _db.ContactChangeLogEntries.RemoveRange(contact.ChangeLogEntries);

            // Remove the contact
            _db.Contacts.Remove(contact);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(request.Id);
        }
    }
}

