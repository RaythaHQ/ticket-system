using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class CloseTicket
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
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ICurrentUser currentUser, ITicketPermissionService permissionService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageTickets();

            var ticket = await _db.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            if (ticket.Status == TicketStatus.CLOSED)
            {
                return new CommandResponseDto<long>(ticket.Id);
            }

            var oldStatus = ticket.Status;
            ticket.Status = TicketStatus.CLOSED;
            ticket.ClosedAt = DateTime.UtcNow;

            if (ticket.ResolvedAt == null)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }

            // Update SLA status if applicable
            if (!string.IsNullOrEmpty(ticket.SlaStatus) && ticket.SlaStatus != SlaStatus.BREACHED)
            {
                ticket.SlaStatus = SlaStatus.COMPLETED;
            }

            var changes = new Dictionary<string, object>
            {
                ["Status"] = new { OldValue = oldStatus, NewValue = TicketStatus.CLOSED }
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = "Ticket closed"
            };
            ticket.ChangeLogEntries.Add(changeLog);

            ticket.AddDomainEvent(new TicketClosedEvent(ticket));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

