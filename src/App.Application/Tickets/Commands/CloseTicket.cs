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
        private readonly ITicketConfigService _configService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ITicketPermissionService permissionService,
            ITicketConfigService configService
        )
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

            var ticket = await _db.Tickets.FirstOrDefaultAsync(
                t => t.Id == request.Id,
                cancellationToken
            );

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Check if already closed using status type
            var currentStatusConfig = await _configService.GetStatusByDeveloperNameAsync(
                ticket.Status,
                cancellationToken
            );
            if (currentStatusConfig?.IsClosedType == true)
            {
                return new CommandResponseDto<long>(ticket.Id);
            }

            // Find the first closed-type status (preferably one named "closed")
            var closedStatuses = await _configService.GetActiveStatusesAsync(cancellationToken);
            var closedStatus =
                closedStatuses.FirstOrDefault(s => s.DeveloperName == "closed")
                ?? closedStatuses.FirstOrDefault(s => s.IsClosedType);

            if (closedStatus == null)
            {
                throw new BusinessException(
                    "No closed-type status is configured. Please configure at least one closed status."
                );
            }

            var oldStatus = ticket.Status;
            ticket.Status = closedStatus.DeveloperName;
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

            var oldLabel = currentStatusConfig?.Label ?? oldStatus;

            var changes = new Dictionary<string, object>
            {
                ["Status"] = new { OldValue = oldStatus, NewValue = closedStatus.DeveloperName },
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = $"Ticket closed (status changed from {oldLabel} to {closedStatus.Label})",
            };
            ticket.ChangeLogEntries.Add(changeLog);

            ticket.AddDomainEvent(new TicketClosedEvent(ticket, _currentUser.UserId?.Guid));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
