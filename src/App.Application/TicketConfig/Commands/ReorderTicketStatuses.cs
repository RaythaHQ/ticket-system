using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class ReorderTicketStatuses
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        /// <summary>
        /// List of status IDs in the desired order.
        /// </summary>
        public List<ShortGuid> OrderedIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderedIds)
                .NotEmpty()
                .WithMessage("Must provide at least one status ID.");
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
            var statuses = await _db.TicketStatusConfigs.ToListAsync(cancellationToken);
            var orderedGuids = request.OrderedIds.Select(id => id.Guid).ToList();

            // Validate: the first status in the new order must be Open type
            if (orderedGuids.Count > 0)
            {
                var firstStatus = statuses.FirstOrDefault(s => s.Id == orderedGuids[0]);
                if (firstStatus != null && firstStatus.StatusType == TicketStatusType.CLOSED)
                {
                    throw new BusinessException("The first status must be of type 'Open' as it is used as the default for new tickets.");
                }
            }

            for (int i = 0; i < orderedGuids.Count; i++)
            {
                var status = statuses.FirstOrDefault(s => s.Id == orderedGuids[i]);
                if (status != null)
                {
                    status.SortOrder = i + 1;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<bool>(true);
        }
    }
}

