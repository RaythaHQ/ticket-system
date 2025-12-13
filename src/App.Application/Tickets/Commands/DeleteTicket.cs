using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class DeleteTicket
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
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Require Can Manage Tickets permission to delete tickets
            _permissionService.RequireCanManageTickets();

            var ticket = await _db
                .Tickets.Include(t => t.Comments)
                .Include(t => t.ChangeLogEntries)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Remove related entities
            _db.TicketComments.RemoveRange(ticket.Comments);
            _db.TicketChangeLogEntries.RemoveRange(ticket.ChangeLogEntries);

            // Remove the ticket
            _db.Tickets.Remove(ticket);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(request.Id);
        }
    }
}
