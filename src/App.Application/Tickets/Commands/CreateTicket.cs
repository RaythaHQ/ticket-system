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

public class CreateTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public string Title { get; init; } = null!;
        public string? Description { get; init; }
        /// <summary>
        /// Priority developer name. If null/empty, the default priority will be used.
        /// </summary>
        public string? Priority { get; init; }
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
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

            // Validate priority against configured active priorities
            RuleFor(x => x.Priority)
                .MustAsync(async (priority, cancellationToken) =>
                {
                    if (string.IsNullOrEmpty(priority)) return true; // Will use default
                    return await db.TicketPriorityConfigs
                        .AnyAsync(p => p.DeveloperName == priority.ToLower() && p.IsActive, cancellationToken);
                })
                .WithMessage("Invalid or inactive priority value.");

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

            RuleFor(x => x.ContactId)
                .MustAsync(async (contactId, cancellationToken) =>
                {
                    if (!contactId.HasValue) return true;
                    return await db.Contacts.AsNoTracking().AnyAsync(c => c.Id == contactId.Value, cancellationToken);
                })
                .WithMessage("Contact not found.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IRoundRobinService _roundRobinService;
        private readonly ITicketConfigService _configService;

        public Handler(IAppDbContext db, ICurrentUser currentUser, IRoundRobinService roundRobinService, ITicketConfigService configService)
        {
            _db = db;
            _currentUser = currentUser;
            _roundRobinService = roundRobinService;
            _configService = configService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            Guid? assigneeId = request.AssigneeId?.Guid;
            bool wasAutoAssigned = false;

            // If ticket is assigned to a team but no assignee specified, try round-robin
            if (request.OwningTeamId.HasValue && !request.AssigneeId.HasValue)
            {
                var autoAssignee = await _roundRobinService.GetNextAssigneeAsync(request.OwningTeamId.Value, cancellationToken);
                if (autoAssignee.HasValue)
                {
                    assigneeId = autoAssignee.Value.Guid;
                    wasAutoAssigned = true;
                }
            }

            // Get default status and priority from config
            var defaultStatus = await _configService.GetDefaultStatusAsync(cancellationToken);
            var defaultPriority = await _configService.GetDefaultPriorityAsync(cancellationToken);

            var ticket = new Ticket
            {
                Title = request.Title,
                Description = request.Description,
                Status = defaultStatus.DeveloperName,
                Priority = string.IsNullOrEmpty(request.Priority) ? defaultPriority.DeveloperName : request.Priority.ToLower(),
                Category = request.Category,
                Tags = request.Tags ?? new List<string>(),
                OwningTeamId = request.OwningTeamId?.Guid,
                AssigneeId = assigneeId,
                ContactId = request.ContactId,
                CreatedByStaffId = _currentUser.UserId?.Guid
            };

            ticket.AddDomainEvent(new TicketCreatedEvent(ticket));

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);

            // Record round-robin assignment if used
            if (wasAutoAssigned && assigneeId.HasValue && request.OwningTeamId.HasValue)
            {
                var membership = await _db.TeamMemberships
                    .FirstOrDefaultAsync(m => m.TeamId == request.OwningTeamId.Value.Guid && m.StaffAdminId == assigneeId.Value, cancellationToken);
                if (membership != null)
                {
                    membership.LastAssignedAt = DateTime.UtcNow;
                }
            }

            // Add initial change log entry after save so we have the TicketId
            var changeLogMessage = wasAutoAssigned
                ? "Ticket created (auto-assigned via round-robin)"
                : "Ticket created";

            var changeLog = new TicketChangeLogEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                Message = changeLogMessage
            };

            _db.TicketChangeLogEntries.Add(changeLog);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
