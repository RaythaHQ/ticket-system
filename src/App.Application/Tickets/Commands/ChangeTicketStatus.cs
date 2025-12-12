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

public class ChangeTicketStatus
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public string NewStatus { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.NewStatus)
                .Must(s => TicketStatus.SupportedTypes.Any(t => t.DeveloperName == s))
                .WithMessage("Invalid status value.");
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

            var oldStatus = ticket.Status;
            if (oldStatus == request.NewStatus)
            {
                return new CommandResponseDto<long>(ticket.Id);
            }

            ticket.Status = request.NewStatus;

            // Handle resolved timestamp
            if (request.NewStatus == TicketStatus.RESOLVED && ticket.ResolvedAt == null)
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }
            else if (request.NewStatus != TicketStatus.RESOLVED && request.NewStatus != TicketStatus.CLOSED)
            {
                ticket.ResolvedAt = null;
            }

            // Add change log entry
            var changes = new Dictionary<string, object>
            {
                ["Status"] = new { OldValue = oldStatus, NewValue = request.NewStatus }
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = $"Status changed from {TicketStatus.From(oldStatus).Label} to {TicketStatus.From(request.NewStatus).Label}"
            };
            ticket.ChangeLogEntries.Add(changeLog);

            ticket.AddDomainEvent(new TicketStatusChangedEvent(ticket, oldStatus, request.NewStatus));

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

