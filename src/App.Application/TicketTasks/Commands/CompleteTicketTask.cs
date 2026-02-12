using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Commands;

public class CompleteTicketTask
{
    public record Command : LoggableRequest<CommandResponseDto<TicketTaskDto>>
    {
        public ShortGuid TaskId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator() { }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<TicketTaskDto>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<TicketTaskDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var task = await _db.TicketTasks
                .Include(t => t.DependsOnTask)
                .Include(t => t.DependentTasks)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId.Guid, cancellationToken);

            if (task == null)
                throw new NotFoundException("TicketTask", request.TaskId.ToString());

            // Check if task is blocked
            if (task.DependsOnTaskId != null && task.DependsOnTask?.Status != TicketTaskStatus.CLOSED)
            {
                return new CommandResponseDto<TicketTaskDto>(
                    "TaskId", "Cannot complete a blocked task. Resolve the dependency first.");
            }

            if (task.Status == TicketTaskStatus.CLOSED)
            {
                return new CommandResponseDto<TicketTaskDto>(
                    "TaskId", "Task is already completed.");
            }

            task.Status = TicketTaskStatus.CLOSED;
            task.ClosedAt = DateTime.UtcNow;
            task.ClosedByStaffId = _currentUser.UserIdAsGuid;

            task.AddDomainEvent(new TicketTaskCompletedEvent(task));

            // Check for dependent tasks that become unblocked
            foreach (var dependentTask in task.DependentTasks)
            {
                if (dependentTask.Status == TicketTaskStatus.OPEN)
                {
                    dependentTask.AddDomainEvent(new TicketTaskUnblockedEvent(dependentTask));
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            // Reload for DTO
            var completed = await _db.TicketTasks
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .FirstAsync(t => t.Id == task.Id, cancellationToken);

            return new CommandResponseDto<TicketTaskDto>(TicketTaskDto.MapFrom(completed));
        }
    }
}
