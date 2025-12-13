using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class UpdateTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public string Title { get; init; } = null!;
        public string? Description { get; init; }
        public string Priority { get; init; } = null!;
        public string? Category { get; init; }
        public List<string>? Tags { get; init; }
        public ShortGuid? OwningTeamId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
        public long? ContactId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Priority)
                .MustAsync(async (priority, cancellationToken) =>
                {
                    return await db.TicketPriorityConfigs
                        .AsNoTracking()
                        .AnyAsync(p => p.DeveloperName == priority && p.IsActive, cancellationToken);
                })
                .WithMessage("Invalid priority value.");

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
        private readonly ISlaService _slaService;
        private readonly IRoundRobinService _roundRobinService;
        private readonly ITicketConfigService _ticketConfigService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ITicketPermissionService permissionService,
            ISlaService slaService,
            IRoundRobinService roundRobinService,
            ITicketConfigService ticketConfigService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _slaService = slaService;
            _roundRobinService = roundRobinService;
            _ticketConfigService = ticketConfigService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var ticket = await _db
                .Tickets.Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Check permission - user can edit if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );

            var changes = new Dictionary<string, object>();
            var oldAssigneeId = ticket.AssigneeId;
            var oldTeamId = ticket.OwningTeamId;
            bool wasAutoAssigned = false;
            
            // Handle round-robin auto-assignment:
            // If team is being set/changed AND no assignee is specified, try round-robin
            Guid? effectiveAssigneeId = request.AssigneeId?.Guid;
            if (request.OwningTeamId.HasValue && !request.AssigneeId.HasValue)
            {
                // Only trigger round-robin if team is new or changing
                if (ticket.OwningTeamId != request.OwningTeamId.Value.Guid)
                {
                    var autoAssignee = await _roundRobinService.GetNextAssigneeAsync(request.OwningTeamId.Value, cancellationToken);
                    if (autoAssignee.HasValue)
                    {
                        effectiveAssigneeId = autoAssignee.Value.Guid;
                        wasAutoAssigned = true;
                    }
                }
            }

            // Track changes
            if (ticket.Title != request.Title)
            {
                changes["Title"] = new { OldValue = ticket.Title, NewValue = request.Title };
                ticket.Title = request.Title;
            }

            if (ticket.Description != request.Description)
            {
                changes["Description"] = new
                {
                    OldValue = ticket.Description ?? "",
                    NewValue = request.Description ?? "",
                };
                ticket.Description = request.Description;
            }

            if (ticket.Priority != request.Priority)
            {
                changes["Priority"] = new
                {
                    OldValue = ticket.Priority,
                    NewValue = request.Priority,
                };
                ticket.Priority = request.Priority;
            }

            if (ticket.Category != request.Category)
            {
                changes["Category"] = new
                {
                    OldValue = ticket.Category ?? "",
                    NewValue = request.Category ?? "",
                };
                ticket.Category = request.Category;
            }

            if (ticket.OwningTeamId != request.OwningTeamId?.Guid)
            {
                changes["OwningTeamId"] = new
                {
                    OldValue = ticket.OwningTeamId?.ToString() ?? "",
                    NewValue = request.OwningTeamId?.Guid.ToString() ?? "",
                };
                ticket.OwningTeamId = request.OwningTeamId?.Guid;
            }

            if (ticket.AssigneeId != effectiveAssigneeId)
            {
                changes["AssigneeId"] = new
                {
                    OldValue = ticket.AssigneeId?.ToString() ?? "",
                    NewValue = effectiveAssigneeId?.ToString() ?? "",
                };
                ticket.AssigneeId = effectiveAssigneeId;
            }

            if (ticket.ContactId != request.ContactId)
            {
                changes["ContactId"] = new
                {
                    OldValue = ticket.ContactId?.ToString() ?? "",
                    NewValue = request.ContactId?.ToString() ?? "",
                };
                ticket.ContactId = request.ContactId;
            }

            ticket.Tags = request.Tags ?? new List<string>();

            // Check if SLA-relevant fields changed
            var slaRelevantFieldsChanged =
                changes.ContainsKey("Priority")
                || changes.ContainsKey("Category")
                || changes.ContainsKey("OwningTeamId");

            if (changes.Any())
            {
                // Build descriptive message with before/after values
                var messageParts = new List<string>();

                // Determine team IDs for assignee change description
                // Use the team that was/will be on the ticket at the time of the assignee change
                var oldAssigneeTeamId = oldTeamId; // Team before any changes
                var newAssigneeTeamId = changes.ContainsKey("OwningTeamId")
                    ? request.OwningTeamId?.Guid
                    : oldTeamId; // If team changed, use new team; otherwise keep old team

                foreach (var change in changes)
                {
                    var fieldName = change.Key;
                    var changeObj = JsonSerializer.Deserialize<JsonElement>(
                        JsonSerializer.Serialize(change.Value)
                    );
                    var oldValue = changeObj.GetProperty("OldValue").GetString() ?? "";
                    var newValue = changeObj.GetProperty("NewValue").GetString() ?? "";

                    string description = fieldName switch
                    {
                        "Title" => $"Title changed from \"{oldValue}\" to \"{newValue}\"",
                        "Description" => $"Description updated",
                        "Priority" => await GetPriorityChangeDescription(
                            oldValue,
                            newValue,
                            cancellationToken
                        ),
                        "Category" => $"Category changed from \"{oldValue}\" to \"{newValue}\"",
                        "OwningTeamId" => await GetTeamChangeDescription(
                            oldValue,
                            newValue,
                            cancellationToken
                        ),
                        "AssigneeId" => await GetAssigneeChangeDescription(
                            oldValue,
                            newValue,
                            oldAssigneeTeamId,
                            newAssigneeTeamId,
                            wasAutoAssigned,
                            cancellationToken
                        ),
                        "ContactId" => await GetContactChangeDescription(
                            oldValue,
                            newValue,
                            cancellationToken
                        ),
                        _ => $"{fieldName} changed from \"{oldValue}\" to \"{newValue}\"",
                    };
                    messageParts.Add(description);
                }

                // Add change log entry
                var changeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserId?.Guid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = string.Join("; ", messageParts),
                };
                ticket.ChangeLogEntries.Add(changeLog);

                // Raise assignment event if assignee or team changed
                if (
                    oldAssigneeId != effectiveAssigneeId
                    || oldTeamId != request.OwningTeamId?.Guid
                )
                {
                    ticket.AddDomainEvent(
                        new TicketAssignedEvent(
                            ticket,
                            oldAssigneeId,
                            effectiveAssigneeId,
                            oldTeamId,
                            request.OwningTeamId?.Guid
                        )
                    );
                }

                // Record round-robin assignment if used
                if (wasAutoAssigned && effectiveAssigneeId.HasValue && request.OwningTeamId.HasValue)
                {
                    var membership = await _db.TeamMemberships
                        .FirstOrDefaultAsync(m => m.TeamId == request.OwningTeamId.Value.Guid && m.StaffAdminId == effectiveAssigneeId.Value, cancellationToken);
                    if (membership != null)
                    {
                        membership.LastAssignedAt = DateTime.UtcNow;
                    }
                }
            }

            // Re-evaluate SLA if relevant fields changed
            if (slaRelevantFieldsChanged)
            {
                var previousSlaId = ticket.SlaRuleId;
                await _slaService.EvaluateAndAssignSlaAsync(ticket, cancellationToken);

                if (ticket.SlaRuleId != previousSlaId)
                {
                    var slaChangeLog = new TicketChangeLogEntry
                    {
                        TicketId = ticket.Id,
                        ActorStaffId = _currentUser.UserId?.Guid,
                        Message = ticket.SlaRuleId.HasValue
                            ? $"SLA rule re-evaluated and updated"
                            : $"SLA rule removed after re-evaluation",
                    };
                    ticket.ChangeLogEntries.Add(slaChangeLog);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }

        private async Task<string> GetPriorityChangeDescription(
            string oldValue,
            string newValue,
            CancellationToken cancellationToken
        )
        {
            var priorities = await _ticketConfigService.GetAllPrioritiesAsync(true, cancellationToken);
            
            var oldPriority = priorities.FirstOrDefault(p => p.DeveloperName == oldValue);
            var newPriority = priorities.FirstOrDefault(p => p.DeveloperName == newValue);
            
            var oldLabel = oldPriority?.Label ?? oldValue ?? "None";
            var newLabel = newPriority?.Label ?? newValue ?? "None";
            
            return $"Priority changed from {oldLabel} to {newLabel}";
        }

        private async Task<string> GetTeamChangeDescription(
            string oldValue,
            string newValue,
            CancellationToken cancellationToken
        )
        {
            var oldName = "None";
            if (!string.IsNullOrEmpty(oldValue) && Guid.TryParse(oldValue, out var oldGuid))
            {
                var oldTeam = await _db
                    .Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == oldGuid, cancellationToken);
                oldName = oldTeam?.Name ?? "Unknown";
            }

            var newName = "None";
            if (!string.IsNullOrEmpty(newValue) && Guid.TryParse(newValue, out var newGuid))
            {
                var newTeam = await _db
                    .Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == newGuid, cancellationToken);
                newName = newTeam?.Name ?? "Unknown";
            }

            return $"Team changed from {oldName} to {newName}";
        }

        private async Task<string> GetAssigneeChangeDescription(
            string oldValue,
            string newValue,
            Guid? oldTeamId,
            Guid? newTeamId,
            bool wasAutoAssigned,
            CancellationToken cancellationToken
        )
        {
            var oldDisplay = "Unassigned";
            if (!string.IsNullOrEmpty(oldValue) && Guid.TryParse(oldValue, out var oldGuid))
            {
                var oldAssignee = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == oldGuid, cancellationToken);

                if (oldAssignee != null)
                {
                    if (oldTeamId.HasValue)
                    {
                        var oldTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == oldTeamId.Value, cancellationToken);
                        oldDisplay = $"{oldTeam?.Name ?? "Unknown"} / {oldAssignee.FullName}";
                    }
                    else
                    {
                        oldDisplay = oldAssignee.FullName;
                    }
                }
            }
            else if (oldTeamId.HasValue)
            {
                // Team assigned but no individual
                var oldTeam = await _db
                    .Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == oldTeamId.Value, cancellationToken);
                oldDisplay = $"{oldTeam?.Name ?? "Unknown"} / Anyone";
            }

            var newDisplay = "Unassigned";
            if (!string.IsNullOrEmpty(newValue) && Guid.TryParse(newValue, out var newGuid))
            {
                var newAssignee = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == newGuid, cancellationToken);

                if (newAssignee != null)
                {
                    if (newTeamId.HasValue)
                    {
                        var newTeam = await _db
                            .Teams.AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == newTeamId.Value, cancellationToken);
                        newDisplay = $"{newTeam?.Name ?? "Unknown"} / {newAssignee.FullName}";
                    }
                    else
                    {
                        newDisplay = newAssignee.FullName;
                    }
                }
            }
            else if (newTeamId.HasValue)
            {
                // Team assigned but no individual
                var newTeam = await _db
                    .Teams.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == newTeamId.Value, cancellationToken);
                newDisplay = $"{newTeam?.Name ?? "Unknown"} / Anyone";
            }

            var suffix = wasAutoAssigned ? " (auto-assigned via round-robin)" : "";
            return $"Assignee changed from {oldDisplay} to {newDisplay}{suffix}";
        }

        private async Task<string> GetContactChangeDescription(
            string oldValue,
            string newValue,
            CancellationToken cancellationToken
        )
        {
            var oldName = "None";
            if (!string.IsNullOrEmpty(oldValue) && long.TryParse(oldValue, out var oldId))
            {
                var oldContact = await _db
                    .Contacts.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == oldId, cancellationToken);
                oldName = oldContact?.FullName ?? "Unknown";
            }

            var newName = "None";
            if (!string.IsNullOrEmpty(newValue) && long.TryParse(newValue, out var newId))
            {
                var newContact = await _db
                    .Contacts.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == newId, cancellationToken);
                newName = newContact?.FullName ?? "Unknown";
            }

            return $"Contact changed from {oldName} to {newName}";
        }
    }
}
