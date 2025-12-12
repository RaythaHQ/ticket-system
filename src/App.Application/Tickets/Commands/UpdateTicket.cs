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
                .Must(p => TicketPriority.SupportedTypes.Any(t => t.DeveloperName == p))
                .WithMessage("Invalid priority value.");

            RuleFor(x => x.OwningTeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    if (!teamId.HasValue) return true;
                    return await db.Teams.AsNoTracking().AnyAsync(t => t.Id == teamId.Value.Guid, cancellationToken);
                })
                .WithMessage("Team not found.");

            RuleFor(x => x.AssigneeId)
                .MustAsync(async (assigneeId, cancellationToken) =>
                {
                    if (!assigneeId.HasValue) return true;
                    return await db.Users.AsNoTracking().AnyAsync(u => u.Id == assigneeId.Value.Guid && u.IsActive, cancellationToken);
                })
                .WithMessage("Assignee not found or inactive.");

            // Validate that if both team and assignee are provided, assignee is a member of that team
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (!cmd.OwningTeamId.HasValue || !cmd.AssigneeId.HasValue) return true;
                    return await db.TeamMemberships.AsNoTracking()
                        .AnyAsync(m => m.TeamId == cmd.OwningTeamId.Value.Guid && m.StaffAdminId == cmd.AssigneeId.Value.Guid, cancellationToken);
                })
                .WithMessage("Assignee must be a member of the specified team.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;
        private readonly ISlaService _slaService;

        public Handler(IAppDbContext db, ICurrentUser currentUser, ITicketPermissionService permissionService, ISlaService slaService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _slaService = slaService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var ticket = await _db.Tickets
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Check permission - user can edit if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(ticket.AssigneeId, ticket.OwningTeamId, cancellationToken);

            var changes = new Dictionary<string, object>();
            var oldAssigneeId = ticket.AssigneeId;
            var oldTeamId = ticket.OwningTeamId;

            // Track changes
            if (ticket.Title != request.Title)
            {
                changes["Title"] = new { OldValue = ticket.Title, NewValue = request.Title };
                ticket.Title = request.Title;
            }

            if (ticket.Description != request.Description)
            {
                changes["Description"] = new { OldValue = ticket.Description ?? "", NewValue = request.Description ?? "" };
                ticket.Description = request.Description;
            }

            if (ticket.Priority != request.Priority)
            {
                changes["Priority"] = new { OldValue = ticket.Priority, NewValue = request.Priority };
                ticket.Priority = request.Priority;
            }

            if (ticket.Category != request.Category)
            {
                changes["Category"] = new { OldValue = ticket.Category ?? "", NewValue = request.Category ?? "" };
                ticket.Category = request.Category;
            }

            if (ticket.OwningTeamId != request.OwningTeamId?.Guid)
            {
                changes["OwningTeamId"] = new { OldValue = ticket.OwningTeamId?.ToString() ?? "", NewValue = request.OwningTeamId?.ToString() ?? "" };
                ticket.OwningTeamId = request.OwningTeamId?.Guid;
            }

            if (ticket.AssigneeId != request.AssigneeId?.Guid)
            {
                changes["AssigneeId"] = new { OldValue = ticket.AssigneeId?.ToString() ?? "", NewValue = request.AssigneeId?.ToString() ?? "" };
                ticket.AssigneeId = request.AssigneeId?.Guid;
            }

            if (ticket.ContactId != request.ContactId)
            {
                changes["ContactId"] = new { OldValue = ticket.ContactId?.ToString() ?? "", NewValue = request.ContactId?.ToString() ?? "" };
                ticket.ContactId = request.ContactId;
            }

            ticket.Tags = request.Tags ?? new List<string>();

            // Check if SLA-relevant fields changed
            var slaRelevantFieldsChanged = changes.ContainsKey("Priority") ||
                                           changes.ContainsKey("Category") ||
                                           changes.ContainsKey("OwningTeamId");

            if (changes.Any())
            {
                // Add change log entry
                var changeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserId?.Guid,
                    FieldChangesJson = JsonSerializer.Serialize(changes),
                    Message = $"Ticket updated: {string.Join(", ", changes.Keys)}"
                };
                ticket.ChangeLogEntries.Add(changeLog);

                // Raise assignment event if assignee or team changed
                if (oldAssigneeId != request.AssigneeId?.Guid || oldTeamId != request.OwningTeamId?.Guid)
                {
                    ticket.AddDomainEvent(new TicketAssignedEvent(ticket, oldAssigneeId, request.AssigneeId?.Guid, oldTeamId, request.OwningTeamId?.Guid));
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
                            : $"SLA rule removed after re-evaluation"
                    };
                    ticket.ChangeLogEntries.Add(slaChangeLog);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

