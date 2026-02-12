using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Commands;

public class ReorderTicketTasks
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        public long TicketId { get; init; }
        public List<ShortGuid> OrderedIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderedIds)
                .NotEmpty()
                .WithMessage("Must provide at least one task ID.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            var tasks = await _db.TicketTasks
                .Where(t => t.TicketId == request.TicketId)
                .ToListAsync(cancellationToken);

            var orderedGuids = request.OrderedIds.Select(id => id.Guid).ToList();

            for (int i = 0; i < orderedGuids.Count; i++)
            {
                var task = tasks.FirstOrDefault(t => t.Id == orderedGuids[i]);
                if (task != null)
                {
                    task.SortOrder = i + 1;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}
