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

public class AssignTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public Guid? AssigneeId { get; init; }
        public Guid? OwningTeamId { get; init; }
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
                    return await db.Users.AsNoTracking().AnyAsync(u => u.Id == assigneeId.Value, cancellationToken);
                })
                .WithMessage("Assignee not found.");

            RuleFor(x => x.OwningTeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    if (!teamId.HasValue) return true;
                    return await db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId.Value, cancellationToken);
                })
                .WithMessage("Team not found.");
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
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            var oldAssigneeId = ticket.AssigneeId;
            var oldTeamId = ticket.OwningTeamId;
            var changes = new Dictionary<string, object>();

            if (oldAssigneeId != request.AssigneeId)
            {
                var oldAssigneeName = ticket.Assignee?.FullName ?? "Unassigned";
                var newAssigneeName = "Unassigned";
                if (request.AssigneeId.HasValue)
                {
                    var newAssignee = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.AssigneeId.Value, cancellationToken);
                    newAssigneeName = newAssignee?.FullName ?? "Unknown";
                }

                changes["AssigneeId"] = new { OldValue = oldAssigneeId?.ToString() ?? "", NewValue = request.AssigneeId?.ToString() ?? "" };
                ticket.AssigneeId = request.AssigneeId;
            }

            if (oldTeamId != request.OwningTeamId)
            {
                changes["OwningTeamId"] = new { OldValue = oldTeamId?.ToString() ?? "", NewValue = request.OwningTeamId?.ToString() ?? "" };
                ticket.OwningTeamId = request.OwningTeamId;
            }

            if (changes.Any())
            {
                var changeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserId?.Guid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = "Ticket assignment changed"
                };
                ticket.ChangeLogEntries.Add(changeLog);

                ticket.AddDomainEvent(new TicketAssignedEvent(ticket, oldAssigneeId, request.AssigneeId, oldTeamId, request.OwningTeamId));
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

