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
                .MustAsync(
                    async (assigneeId, cancellationToken) =>
                    {
                        if (!assigneeId.HasValue)
                            return true;
                        return await db
                            .Users.AsNoTracking()
                            .AnyAsync(
                                u => u.Id == assigneeId.Value.Guid && u.IsActive,
                                cancellationToken
                            );
                    }
                )
                .WithMessage("Assignee not found or inactive.");

            RuleFor(x => x.OwningTeamId)
                .MustAsync(
                    async (teamId, cancellationToken) =>
                    {
                        if (!teamId.HasValue)
                            return true;
                        return await db
                            .Teams.AsNoTracking()
                            .AnyAsync(t => t.Id == teamId.Value.Guid, cancellationToken);
                    }
                )
                .WithMessage("Team not found.");

            // Validate that if both team and assignee are provided, assignee is a member of that team
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        if (!cmd.OwningTeamId.HasValue || !cmd.AssigneeId.HasValue)
                            return true;
                        return await db
                            .TeamMemberships.AsNoTracking()
                            .AnyAsync(
                                m =>
                                    m.TeamId == cmd.OwningTeamId.Value.Guid
                                    && m.StaffAdminId == cmd.AssigneeId.Value.Guid,
                                cancellationToken
                            );
                    }
                )
                .WithMessage("Assignee must be a member of the specified team.");
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
            IRoundRobinService roundRobinService
        )
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

            var ticket = await _db
                .Tickets.Include(t => t.Assignee)
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
            if (
                request.OwningTeamId.HasValue
                && request.OwningTeamId.Value.Guid != oldTeamId
                && !request.AssigneeId.HasValue
            )
            {
                var autoAssignee = await _roundRobinService.GetNextAssigneeAsync(
                    request.OwningTeamId.Value,
                    cancellationToken
                );
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
                    var newAssignee = await _db.Users.FirstOrDefaultAsync(
                        u => u.Id == newAssigneeId.Value,
                        cancellationToken
                    );
                    newAssigneeName = newAssignee?.FullName ?? "Unknown";
                }

                changes["AssigneeId"] = new
                {
                    OldValue = oldAssigneeId?.ToString() ?? "",
                    NewValue = newAssigneeId?.ToString() ?? "",
                };
                ticket.AssigneeId = newAssigneeId;
                ticket.AssignedAt = newAssigneeId.HasValue ? DateTime.UtcNow : null;
            }

            if (oldTeamId != request.OwningTeamId?.Guid)
            {
                changes["OwningTeamId"] = new
                {
                    OldValue = oldTeamId?.ToString() ?? "",
                    NewValue = request.OwningTeamId?.ToString() ?? "",
                };
                ticket.OwningTeamId = request.OwningTeamId?.Guid;
            }

            if (changes.Any())
            {
                var messageParts = new List<string>();

                // Determine team IDs for assignee change description
                var oldAssigneeTeamId = oldTeamId;
                var newAssigneeTeamId = request.OwningTeamId?.Guid;

                if (oldAssigneeId != newAssigneeId)
                {
                    var oldDisplay = "Unassigned";
                    if (oldAssigneeId.HasValue)
                    {
                        var oldAssignee = await _db
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(
                                u => u.Id == oldAssigneeId.Value,
                                cancellationToken
                            );
                        if (oldAssignee != null)
                        {
                            if (oldAssigneeTeamId.HasValue)
                            {
                                var oldTeam = await _db
                                    .Teams.AsNoTracking()
                                    .FirstOrDefaultAsync(
                                        t => t.Id == oldAssigneeTeamId.Value,
                                        cancellationToken
                                    );
                                oldDisplay =
                                    $"{oldTeam?.Name ?? "Unknown"} / {oldAssignee.FullName}";
                            }
                            else
                            {
                                oldDisplay = oldAssignee.FullName;
                            }
                        }
                    }
                    else if (oldAssigneeTeamId.HasValue)
                    {
                        var oldTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(
                                t => t.Id == oldAssigneeTeamId.Value,
                                cancellationToken
                            );
                        oldDisplay = $"{oldTeam?.Name ?? "Unknown"} / Anyone";
                    }

                    var newDisplay = "Unassigned";
                    if (newAssigneeId.HasValue)
                    {
                        var newAssignee = await _db
                            .Users.AsNoTracking()
                            .FirstOrDefaultAsync(
                                u => u.Id == newAssigneeId.Value,
                                cancellationToken
                            );
                        if (newAssignee != null)
                        {
                            if (newAssigneeTeamId.HasValue)
                            {
                                var newTeam = await _db
                                    .Teams.AsNoTracking()
                                    .FirstOrDefaultAsync(
                                        t => t.Id == newAssigneeTeamId.Value,
                                        cancellationToken
                                    );
                                newDisplay =
                                    $"{newTeam?.Name ?? "Unknown"} / {newAssignee.FullName}";
                            }
                            else
                            {
                                newDisplay = newAssignee.FullName;
                            }
                        }
                    }
                    else if (newAssigneeTeamId.HasValue)
                    {
                        var newTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(
                                t => t.Id == newAssigneeTeamId.Value,
                                cancellationToken
                            );
                        newDisplay = $"{newTeam?.Name ?? "Unknown"} / Anyone";
                    }

                    var assigneeChange = wasAutoAssigned
                        ? $"Assignee changed from {oldDisplay} to {newDisplay} (auto-assigned via round-robin)"
                        : $"Assignee changed from {oldDisplay} to {newDisplay}";
                    messageParts.Add(assigneeChange);
                }

                if (oldTeamId != request.OwningTeamId?.Guid)
                {
                    var oldName = "None";
                    if (oldTeamId.HasValue)
                    {
                        var oldTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == oldTeamId.Value, cancellationToken);
                        oldName = oldTeam?.Name ?? "Unknown";
                    }

                    var newName = "None";
                    if (request.OwningTeamId.HasValue)
                    {
                        var newTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(
                                t => t.Id == request.OwningTeamId.Value.Guid,
                                cancellationToken
                            );
                        newName = newTeam?.Name ?? "Unknown";
                    }

                    messageParts.Add($"Team changed from {oldName} to {newName}");
                }

                var message = messageParts.Any()
                    ? string.Join("; ", messageParts)
                    : "Ticket assignment changed";

                var changeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserIdAsGuid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = message,
                };
                ticket.ChangeLogEntries.Add(changeLog);

                ticket.AddDomainEvent(
                    new TicketAssignedEvent(
                        ticket,
                        oldAssigneeId,
                        newAssigneeId,
                        oldTeamId,
                        request.OwningTeamId?.Guid,
                        _currentUser.UserIdAsGuid
                    )
                );

                // Record round-robin assignment if used
                if (wasAutoAssigned && newAssigneeId.HasValue && request.OwningTeamId.HasValue)
                {
                    var membership = await _db.TeamMemberships.FirstOrDefaultAsync(
                        m =>
                            m.TeamId == request.OwningTeamId.Value.Guid
                            && m.StaffAdminId == newAssigneeId.Value,
                        cancellationToken
                    );
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
