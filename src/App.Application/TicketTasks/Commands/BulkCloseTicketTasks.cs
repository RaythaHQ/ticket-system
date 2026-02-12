using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Commands;

public class BulkCloseTicketTasks
{
    public record Command : LoggableRequest<CommandResponseDto<int>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<int>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<int>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var openTasks = await _db.TicketTasks
                .Where(t => t.TicketId == request.TicketId && t.Status != TicketTaskStatus.CLOSED)
                .ToListAsync(cancellationToken);

            var closedCount = 0;
            foreach (var task in openTasks)
            {
                task.Status = TicketTaskStatus.CLOSED;
                task.ClosedAt = DateTime.UtcNow;
                task.ClosedByStaffId = _currentUser.UserIdAsGuid;
                task.AddDomainEvent(new TicketTaskCompletedEvent(task));
                closedCount++;
            }

            if (closedCount > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
            }

            return new CommandResponseDto<int>(closedCount);
        }
    }
}
