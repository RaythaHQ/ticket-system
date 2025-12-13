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

public class ReopenTicket
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
        private readonly ITicketConfigService _configService;

        public Handler(IAppDbContext db, ICurrentUser currentUser, ITicketPermissionService permissionService, ITicketConfigService configService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _configService = configService;
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

            // Check if current status is closed type
            var currentStatusConfig = await _configService.GetStatusByDeveloperNameAsync(ticket.Status, cancellationToken);
            if (currentStatusConfig?.IsOpenType == true)
            {
                // Already open
                return new CommandResponseDto<long>(ticket.Id);
            }

            var oldStatus = ticket.Status;
            
            // Get the default status (first in list, must be Open type)
            var defaultStatus = await _configService.GetDefaultStatusAsync(cancellationToken);
            
            ticket.Status = defaultStatus.DeveloperName;
            ticket.ClosedAt = null;
            ticket.ResolvedAt = null;

            // Reset SLA if it was completed
            if (ticket.SlaStatus == SlaStatus.COMPLETED)
            {
                ticket.SlaStatus = SlaStatus.ON_TRACK;
            }

            var oldLabel = currentStatusConfig?.Label ?? oldStatus;

            var changes = new Dictionary<string, object>
            {
                ["Status"] = new { OldValue = oldStatus, NewValue = defaultStatus.DeveloperName }
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = $"Ticket reopened (status changed from {oldLabel} to {defaultStatus.Label})"
            };
            ticket.ChangeLogEntries.Add(changeLog);

            ticket.AddDomainEvent(new TicketReopenedEvent(ticket));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

