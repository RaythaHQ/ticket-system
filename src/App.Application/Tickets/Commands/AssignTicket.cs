using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class AssignTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public ShortGuid? AssigneeId { get; init; }
        public ShortGuid? OwningTeamId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).GreaterThan(0);

            RuleFor(x => x.AssigneeId)
                .MustAsync(async (assigneeId, cancellationToken) =>
                {
                    if (!assigneeId.HasValue) return true;
                    return await db.Users.AsNoTracking().AnyAsync(u => u.Id == assigneeId.Value.Guid, cancellationToken);
                })
                .WithMessage("Assignee not found.");

            RuleFor(x => x.OwningTeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    if (!teamId.HasValue) return true;
                    return await db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId.Value.Guid, cancellationToken);
                })
                .WithMessage("Team not found.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;
        private readonly IRoundRobinService _roundRobinService;

        public Handler(
            IAppDbContext db, 
            ICurrentUser currentUser, 
            ITicketPermissionService permissionService,
            IRoundRobinService roundRobinService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _roundRobinService = roundRobinService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageTickets();

            var ticket = await _db.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            var oldAssigneeId = ticket.AssigneeId;
            var oldTeamId = ticket.OwningTeamId;
            var changes = new Dictionary<string, object>();
            bool wasAutoAssigned = false;
            Guid? newAssigneeId = request.AssigneeId?.Guid;

            // If team is changing and new team has round-robin, and no assignee specified
            if (request.OwningTeamId.HasValue 
                && request.OwningTeamId.Value.Guid != oldTeamId 
                && !request.AssigneeId.HasValue)
            {
                var autoAssignee = await _roundRobinService.GetNextAssigneeAsync(request.OwningTeamId.Value, cancellationToken);
                if (autoAssignee.HasValue)
                {
                    newAssigneeId = autoAssignee.Value.Guid;
                    wasAutoAssigned = true;
                }
            }

            if (oldAssigneeId != newAssigneeId)
            {
                var oldAssigneeName = ticket.Assignee?.FullName ?? "Unassigned";
                var newAssigneeName = "Unassigned";
                if (newAssigneeId.HasValue)
                {
                    var newAssignee = await _db.Users.FirstOrDefaultAsync(u => u.Id == newAssigneeId.Value, cancellationToken);
                    newAssigneeName = newAssignee?.FullName ?? "Unknown";
                }

                changes["AssigneeId"] = new { OldValue = oldAssigneeId?.ToString() ?? "", NewValue = newAssigneeId?.ToString() ?? "" };
                ticket.AssigneeId = newAssigneeId;
            }

            if (oldTeamId != request.OwningTeamId?.Guid)
            {
                changes["OwningTeamId"] = new { OldValue = oldTeamId?.ToString() ?? "", NewValue = request.OwningTeamId?.ToString() ?? "" };
                ticket.OwningTeamId = request.OwningTeamId?.Guid;
            }

            if (changes.Any())
            {
                var message = wasAutoAssigned
                    ? "Ticket assignment changed (auto-assigned via round-robin)"
                    : "Ticket assignment changed";

                var changeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserId?.Guid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = message
                };
                ticket.ChangeLogEntries.Add(changeLog);

                ticket.AddDomainEvent(new TicketAssignedEvent(ticket, oldAssigneeId, newAssigneeId, oldTeamId, request.OwningTeamId?.Guid));

                // Record round-robin assignment if used
                if (wasAutoAssigned && newAssigneeId.HasValue && request.OwningTeamId.HasValue)
                {
                    var membership = await _db.TeamMemberships
                        .FirstOrDefaultAsync(m => m.TeamId == request.OwningTeamId.Value.Guid && m.StaffAdminId == newAssigneeId.Value, cancellationToken);
                    if (membership != null)
                    {
                        membership.LastAssignedAt = DateTime.UtcNow;
                    }
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
