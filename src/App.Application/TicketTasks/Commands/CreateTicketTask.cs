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

public class CreateTicketTask
{
    public record Command : LoggableRequest<CommandResponseDto<TicketTaskDto>>
    {
        public long TicketId { get; init; }
        public string Title { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);

            RuleFor(x => x.TicketId)
                .MustAsync(async (ticketId, cancellationToken) =>
                {
                    return await db.Tickets.AnyAsync(t => t.Id == ticketId, cancellationToken);
                })
                .WithMessage("Ticket not found.");
        }
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
            var maxSortOrder = await _db.TicketTasks
                .Where(t => t.TicketId == request.TicketId)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;

            var task = new TicketTask
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                Title = request.Title,
                Status = TicketTaskStatus.OPEN,
                SortOrder = maxSortOrder + 1,
                CreatedByStaffId = _currentUser.UserIdAsGuid,
            };

            task.AddDomainEvent(new TicketTaskCreatedEvent(task));

            _db.TicketTasks.Add(task);
            await _db.SaveChangesAsync(cancellationToken);

            // Reload with includes for DTO mapping
            var created = await _db.TicketTasks
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .FirstAsync(t => t.Id == task.Id, cancellationToken);

            return new CommandResponseDto<TicketTaskDto>(TicketTaskDto.MapFrom(created));
        }
    }
}
