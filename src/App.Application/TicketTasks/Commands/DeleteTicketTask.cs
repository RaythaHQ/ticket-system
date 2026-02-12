using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Events;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Commands;

public class DeleteTicketTask
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        public ShortGuid TaskId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator() { }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var task = await _db.TicketTasks
                .Include(t => t.DependentTasks)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId.Guid, cancellationToken);

            if (task == null)
                throw new NotFoundException("TicketTask", request.TaskId.ToString());

            // Unblock dependent tasks by clearing their dependency
            foreach (var dependentTask in task.DependentTasks)
            {
                dependentTask.DependsOnTaskId = null;
                // Only raise unblocked event for open tasks (closed tasks don't need unblocking)
                if (dependentTask.Status == Domain.ValueObjects.TicketTaskStatus.OPEN)
                {
                    dependentTask.AddDomainEvent(new TicketTaskUnblockedEvent(dependentTask));
                }
            }

            // Soft delete
            task.IsDeleted = true;
            task.DeletionTime = DateTime.UtcNow;
            task.DeleterUserId = _currentUser.UserIdAsGuid;

            task.AddDomainEvent(new TicketTaskDeletedEvent(
                task.TicketId,
                task.Title,
                task.AssigneeId));

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}
