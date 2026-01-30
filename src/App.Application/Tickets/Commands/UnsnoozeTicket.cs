using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class UnsnoozeTicket
{
    public record Command : LoggableRequest<CommandResponseDto<UnsnoozeTicketResponseDto>>
    {
        public long TicketId { get; init; }
    }

    public record UnsnoozeTicketResponseDto
    {
        public long TicketId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.TicketId).GreaterThan(0);

            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var ticket = await db.Tickets
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, cancellationToken);

                        return ticket != null;
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

                        return ticket.SnoozedUntil != null;
                    }
                )
                .WithMessage("Ticket is not snoozed.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<UnsnoozeTicketResponseDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;
        private readonly ICurrentOrganization _currentOrganization;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ITicketPermissionService permissionService,
            ICurrentOrganization currentOrganization
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _currentOrganization = currentOrganization;
        }

        public async ValueTask<CommandResponseDto<UnsnoozeTicketResponseDto>> Handle(
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

            // Check permission - user can unsnooze if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );

            if (ticket.SnoozedUntil == null)
            {
                throw new BusinessException("Ticket is not snoozed.");
            }

            var now = DateTime.UtcNow;
            var snoozeDuration =
                ticket.SnoozedAt != null ? now - ticket.SnoozedAt.Value : TimeSpan.Zero;

            // Extend SLA due time by snooze duration if PauseSlaOnSnooze is enabled
            if (
                _currentOrganization.PauseSlaOnSnooze
                && ticket.SlaDueAt != null
                && snoozeDuration > TimeSpan.Zero
            )
            {
                ticket.SlaDueAt = ticket.SlaDueAt.Value.Add(snoozeDuration);
            }

            // Clear snooze fields
            ticket.SnoozedUntil = null;
            ticket.SnoozedAt = null;
            ticket.SnoozedById = null;
            ticket.SnoozedReason = null;
            ticket.UnsnoozedAt = now;

            // Add change log entry
            var changes = new Dictionary<string, object>
            {
                ["SnoozedUntil"] = new { OldValue = "Snoozed", NewValue = (string?)null },
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserIdAsGuid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = "Manually unsnoozed",
            };
            ticket.ChangeLogEntries.Add(changeLog);

            // Raise domain event
            ticket.AddDomainEvent(
                new TicketUnsnoozedEvent(
                    ticket,
                    unsnoozedById: _currentUser.UserIdAsGuid,
                    wasAutoUnsnooze: false,
                    snoozeDuration: snoozeDuration
                )
            );

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<UnsnoozeTicketResponseDto>(
                new UnsnoozeTicketResponseDto { TicketId = ticket.Id }
            );
        }
    }
}
