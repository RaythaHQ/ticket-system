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

public class SnoozeTicket
{
    public record Command : LoggableRequest<CommandResponseDto<SnoozeTicketResponseDto>>
    {
        public long TicketId { get; init; }
        public DateTime SnoozeUntil { get; init; }
        public string? Reason { get; init; }
    }

    public record SnoozeTicketResponseDto
    {
        public long TicketId { get; init; }
        public DateTime SnoozedUntil { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db, ISnoozeConfiguration snoozeConfig)
        {
            RuleFor(x => x.TicketId).GreaterThan(0);

            RuleFor(x => x.SnoozeUntil)
                .NotEmpty()
                .Must(until => until > DateTime.UtcNow)
                .WithMessage("Snooze time must be in the future.")
                .Must(until => until <= DateTime.UtcNow.AddDays(snoozeConfig.MaxDurationDays))
                .WithMessage(
                    $"Snooze duration cannot exceed {snoozeConfig.MaxDurationDays} days."
                );

            RuleFor(x => x.Reason).MaximumLength(500);

            // Validate ticket exists, not closed, and has individual assignee
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var ticket = await db.Tickets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, cancellationToken);

                        if (ticket == null)
                            return false;

                        return true;
                    }
                )
                .WithMessage("Ticket not found.");

            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var ticket = await db.Tickets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, cancellationToken);

                        if (ticket == null)
                            return true; // Let previous rule handle not found

                        return ticket.ClosedAt == null;
                    }
                )
                .WithMessage("Cannot snooze a closed ticket.");

            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var ticket = await db.Tickets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, cancellationToken);

                        if (ticket == null)
                            return true; // Let previous rule handle not found

                        return ticket.AssigneeId != null;
                    }
                )
                .WithMessage("Ticket must be assigned to an individual before snoozing.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<SnoozeTicketResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ICurrentOrganization _currentOrganization;
        private readonly ITicketPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ICurrentOrganization currentOrganization,
            ITicketPermissionService permissionService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _currentOrganization = currentOrganization;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<SnoozeTicketResponseDto>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(
                t => t.Id == request.TicketId,
                cancellationToken
            );

            if (ticket == null)
                throw new NotFoundException("Ticket", request.TicketId);

            // Check permission - user can snooze if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );

            var now = DateTime.UtcNow;

            // Set snooze fields
            ticket.SnoozedUntil = request.SnoozeUntil;
            ticket.SnoozedAt = now;
            ticket.SnoozedById = _currentUser.UserIdAsGuid;
            ticket.SnoozedReason = request.Reason;
            ticket.UnsnoozedAt = null; // Clear any previous unsnooze time

            // Add change log entry
            var changes = new Dictionary<string, object>
            {
                ["SnoozedUntil"] = new { NewValue = request.SnoozeUntil.ToString("O") },
            };

            if (!string.IsNullOrEmpty(request.Reason))
            {
                changes["SnoozedReason"] = new { NewValue = request.Reason };
            }

            var snoozeUntilFormatted = _currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(request.SnoozeUntil);
            var message = $"Snoozed until {snoozeUntilFormatted}";
            if (!string.IsNullOrEmpty(request.Reason))
            {
                message += $": {request.Reason}";
            }

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserIdAsGuid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = message,
            };
            ticket.ChangeLogEntries.Add(changeLog);

            // Raise domain event
            ticket.AddDomainEvent(
                new TicketSnoozedEvent(
                    ticket,
                    request.SnoozeUntil,
                    _currentUser.UserIdAsGuid ?? Guid.Empty,
                    request.Reason
                )
            );

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<SnoozeTicketResponseDto>(
                new SnoozeTicketResponseDto
                {
                    TicketId = ticket.Id,
                    SnoozedUntil = request.SnoozeUntil,
                }
            );
        }
    }
}
