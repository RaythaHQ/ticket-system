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

public class CreateTicket
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        /// <summary>
        /// Optional custom ID. If not specified, an ID will be auto-generated (min 7 digits).
        /// </summary>
        public long? Id { get; init; }
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

            RuleFor(x => x.Id)
                .GreaterThan(0)
                .When(x => x.Id.HasValue)
                .WithMessage("Ticket ID must be a positive number.");
            RuleFor(x => x.Id)
                .MustAsync(async (id, cancellationToken) =>
                {
                    if (!id.HasValue) return true;
                    // Check both active and soft-deleted tickets
                    var exists = await db.Tickets
                        .IgnoreQueryFilters()
                        .AnyAsync(t => t.Id == id.Value, cancellationToken);
                    return !exists;
                })
                .WithMessage("A ticket with this ID already exists.")
                .When(x => x.Id.HasValue);

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
        private readonly INumericIdGenerator _idGenerator;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            IRoundRobinService roundRobinService,
            ITicketConfigService configService,
            INumericIdGenerator idGenerator)
        {
            _db = db;
            _currentUser = currentUser;
            _roundRobinService = roundRobinService;
            _configService = configService;
            _idGenerator = idGenerator;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Determine the ID: use specified ID or generate one
            long ticketId;
            if (request.Id.HasValue)
            {
                // Double-check the ID doesn't exist (belt and suspenders)
                var exists = await _db.Tickets
                    .IgnoreQueryFilters()
                    .AnyAsync(t => t.Id == request.Id.Value, cancellationToken);
                if (exists)
                    throw new BusinessException("A ticket with this ID already exists.");
                ticketId = request.Id.Value;
            }
            else
            {
                // Auto-generate ID (minimum 7 digits)
                ticketId = await _idGenerator.GetNextTicketIdAsync(cancellationToken);
            }

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
                Id = ticketId,
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
