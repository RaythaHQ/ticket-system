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
            var contact = await _db.Contacts.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (contact == null)
                throw new NotFoundException("Contact", request.Id);

            // Soft-delete: mark as deleted but preserve the record for historical reference
            contact.IsDeleted = true;
            contact.DeletionTime = DateTime.UtcNow;
            contact.DeleterUserId = _currentUser.UserIdAsGuid;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(request.Id);
        }
    }
}
