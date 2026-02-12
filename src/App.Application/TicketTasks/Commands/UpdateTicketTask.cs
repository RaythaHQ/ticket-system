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

namespace App.Application.TicketTasks.Commands;

public class UpdateTicketTask
{
    public record Command : LoggableRequest<CommandResponseDto<TicketTaskDto>>
    {
        public ShortGuid TaskId { get; init; }
        public string? Title { get; init; }
        public ShortGuid? AssigneeId { get; init; }
        public ShortGuid? OwningTeamId { get; init; }
        public DateTime? DueAt { get; init; }
        public ShortGuid? DependsOnTaskId { get; init; }

        /// <summary>
        /// When true, explicitly clears the assignee (vs null meaning "don't change").
        /// </summary>
        public bool ClearAssignee { get; init; }
        public bool ClearDueAt { get; init; }
        public bool ClearDependency { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Title)
                .MaximumLength(500)
                .When(x => x.Title != null);

            RuleFor(x => x.AssigneeId)
                .MustAsync(async (assigneeId, cancellationToken) =>
                {
                    if (!assigneeId.HasValue) return true;
                    return await db.Users.AsNoTracking()
                        .AnyAsync(u => u.Id == assigneeId.Value.Guid && u.IsActive, cancellationToken);
                })
                .WithMessage("Assignee not found or inactive.")
                .When(x => x.AssigneeId.HasValue);

            RuleFor(x => x.OwningTeamId)
                .MustAsync(async (teamId, cancellationToken) =>
                {
                    if (!teamId.HasValue) return true;
                    return await db.Teams.AsNoTracking()
                        .AnyAsync(t => t.Id == teamId.Value.Guid, cancellationToken);
                })
                .WithMessage("Team not found.")
                .When(x => x.OwningTeamId.HasValue);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<TicketTaskDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<TicketTaskDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var task = await _db.TicketTasks
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId.Guid, cancellationToken);

            if (task == null)
                throw new NotFoundException("TicketTask", request.TaskId.ToString());

            // Update title
            if (request.Title != null)
            {
                task.Title = request.Title;
            }

            // Update assignee
            if (request.ClearAssignee && request.OwningTeamId.HasValue)
            {
                // Team/Anyone: set team, clear individual assignee
                var prevAssigneeId = task.AssigneeId;
                var prevTeamId = task.OwningTeamId;
                task.AssigneeId = null;
                task.OwningTeamId = request.OwningTeamId.Value.Guid;
                if (prevAssigneeId != null || prevTeamId != task.OwningTeamId)
                {
                    task.AddDomainEvent(new TicketTaskAssignedEvent(task, prevAssigneeId, prevTeamId));
                }
            }
            else if (request.ClearAssignee)
            {
                // Fully unassign
                var prevAssigneeId = task.AssigneeId;
                var prevTeamId = task.OwningTeamId;
                task.AssigneeId = null;
                task.OwningTeamId = null;
                if (prevAssigneeId != null || prevTeamId != null)
                {
                    task.AddDomainEvent(new TicketTaskAssignedEvent(task, prevAssigneeId, prevTeamId));
                }
            }
            else if (request.AssigneeId.HasValue)
            {
                var prevAssigneeId = task.AssigneeId;
                var prevTeamId = task.OwningTeamId;
                task.AssigneeId = request.AssigneeId.Value.Guid;

                // Infer team from assignee membership
                if (request.OwningTeamId.HasValue)
                {
                    task.OwningTeamId = request.OwningTeamId.Value.Guid;
                }
                else
                {
                    var membership = await _db.TeamMemberships.AsNoTracking()
                        .FirstOrDefaultAsync(m => m.StaffAdminId == task.AssigneeId, cancellationToken);
                    task.OwningTeamId = membership?.TeamId;
                }

                if (prevAssigneeId != task.AssigneeId || prevTeamId != task.OwningTeamId)
                {
                    task.AddDomainEvent(new TicketTaskAssignedEvent(task, prevAssigneeId, prevTeamId));
                }
            }
            else if (request.OwningTeamId.HasValue)
            {
                // Team only (no individual specified)
                var prevTeamId = task.OwningTeamId;
                task.OwningTeamId = request.OwningTeamId.Value.Guid;
                if (prevTeamId != task.OwningTeamId)
                {
                    task.AddDomainEvent(new TicketTaskAssignedEvent(task, task.AssigneeId, prevTeamId));
                }
            }

            // Update due date
            if (request.ClearDueAt)
            {
                var prevDueAt = task.DueAt;
                task.DueAt = null;
                if (prevDueAt != null)
                {
                    task.AddDomainEvent(new TicketTaskDueDateChangedEvent(task, prevDueAt));
                }
            }
            else if (request.DueAt.HasValue)
            {
                var prevDueAt = task.DueAt;
                task.DueAt = request.DueAt.Value;
                if (prevDueAt != task.DueAt)
                {
                    task.AddDomainEvent(new TicketTaskDueDateChangedEvent(task, prevDueAt));
                }
            }

            // Update dependency
            if (request.ClearDependency)
            {
                var prevDepId = task.DependsOnTaskId;
                task.DependsOnTaskId = null;
                if (prevDepId != null)
                {
                    task.AddDomainEvent(new TicketTaskDependencyChangedEvent(task, prevDepId));
                }
            }
            else if (request.DependsOnTaskId.HasValue)
            {
                var prevDepId = task.DependsOnTaskId;
                var newDepId = request.DependsOnTaskId.Value.Guid;

                // Cannot depend on self
                if (newDepId == task.Id)
                {
                    return new CommandResponseDto<TicketTaskDto>(
                        "DependsOnTaskId", "A task cannot depend on itself.");
                }

                // Validate: target task must be on the same ticket
                var depTask = await _db.TicketTasks.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == newDepId, cancellationToken);
                if (depTask == null || depTask.TicketId != task.TicketId)
                {
                    return new CommandResponseDto<TicketTaskDto>(
                        "DependsOnTaskId", "Dependency must be a task on the same ticket.");
                }

                // Circular dependency detection: walk the chain from depTask to see if it leads back to task.Id
                var visited = new HashSet<Guid> { task.Id };
                Guid? currentId = newDepId;
                while (currentId.HasValue)
                {
                    if (visited.Contains(currentId.Value))
                    {
                        return new CommandResponseDto<TicketTaskDto>(
                            "DependsOnTaskId", "Circular dependency detected. This would create a dependency loop.");
                    }
                    visited.Add(currentId.Value);
                    currentId = await _db.TicketTasks.AsNoTracking()
                        .Where(t => t.Id == currentId.Value)
                        .Select(t => t.DependsOnTaskId)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                task.DependsOnTaskId = newDepId;
                if (prevDepId != task.DependsOnTaskId)
                {
                    task.AddDomainEvent(new TicketTaskDependencyChangedEvent(task, prevDepId));
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Reload for DTO
            var updated = await _db.TicketTasks
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .FirstAsync(t => t.Id == task.Id, cancellationToken);

            return new CommandResponseDto<TicketTaskDto>(TicketTaskDto.MapFrom(updated));
        }
    }
}
