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

public class ReopenTicketTask
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

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<TicketTaskDto>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var task = await _db.TicketTasks
                .Include(t => t.DependentTasks)
                .FirstOrDefaultAsync(t => t.Id == request.TaskId.Guid, cancellationToken);

            if (task == null)
                throw new NotFoundException("TicketTask", request.TaskId.ToString());

            if (task.Status != TicketTaskStatus.CLOSED)
            {
                return new CommandResponseDto<TicketTaskDto>(
                    "TaskId", "Task is not closed.");
            }

            task.Status = TicketTaskStatus.OPEN;
            task.ClosedAt = null;
            task.ClosedByStaffId = null;

            task.AddDomainEvent(new TicketTaskReopenedEvent(task));

            // Dependent tasks become blocked again (they depend on a now-Open task)
            // No event needed — blocking is derived from the dependency relationship.

            await _db.SaveChangesAsync(cancellationToken);

            // Reload for DTO
            var reopened = await _db.TicketTasks
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .FirstAsync(t => t.Id == task.Id, cancellationToken);

            return new CommandResponseDto<TicketTaskDto>(TicketTaskDto.MapFrom(reopened));
        }
    }
}
