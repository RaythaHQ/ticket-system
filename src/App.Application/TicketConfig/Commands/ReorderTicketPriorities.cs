using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class ReorderTicketPriorities
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        /// <summary>
        /// List of priority IDs in the desired order.
        /// </summary>
        public List<ShortGuid> OrderedIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderedIds)
                .NotEmpty()
                .WithMessage("Must provide at least one priority ID.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketConfigService _configService;

        public Handler(IAppDbContext db, ITicketConfigService configService)
        {
            _db = db;
            _configService = configService;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var priorities = await _db.TicketPriorityConfigs.ToListAsync(cancellationToken);

            var orderedGuids = request.OrderedIds.Select(id => id.Guid).ToList();

            for (int i = 0; i < orderedGuids.Count; i++)
            {
                var priority = priorities.FirstOrDefault(p => p.Id == orderedGuids[i]);
                if (priority != null)
                {
                    priority.SortOrder = i + 1;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<bool>(true);
        }
    }
}

