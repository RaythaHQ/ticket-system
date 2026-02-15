using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
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
        private readonly IAppDbContext _db;

        public Validator(IAppDbContext db)
        {
            _db = db;

            RuleFor(x => x.Id).GreaterThan(0);

            // Prevent deletion of contacts with future active appointments
            RuleFor(x => x.Id)
                .CustomAsync(async (contactId, context, cancellationToken) =>
                {
                    var activeStatuses = new[]
                    {
                        AppointmentStatus.SCHEDULED,
                        AppointmentStatus.CONFIRMED,
                        AppointmentStatus.IN_PROGRESS
                    };

                    var futureActiveAppointmentIds = await _db
                        .Appointments.AsNoTracking()
                        .Where(a =>
                            a.ContactId == contactId
                            && activeStatuses.Contains(a.Status)
                            && a.ScheduledStartTime > DateTime.UtcNow
                        )
                        .Select(a => a.Id)
                        .ToListAsync(cancellationToken);

                    if (futureActiveAppointmentIds.Any())
                    {
                        var codes = futureActiveAppointmentIds
                            .Select(id => $"APT-{id:D4}")
                            .ToList();
                        var codeList = string.Join(", ", codes);

                        context.AddFailure(
                            nameof(Command.Id),
                            $"Cannot delete contact: there are future active appointments ({codeList}). "
                            + "Please cancel or complete those appointments first."
                        );
                    }
                });
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
