using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class ToggleTicketPriorityActive
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public bool IsActive { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            // Cannot deactivate if it's the only active priority
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (cmd.IsActive) return true; // Activating is always allowed

                    var activeCount = await db.TicketPriorityConfigs
                        .CountAsync(p => p.IsActive && p.Id != cmd.Id.Guid, cancellationToken);

                    return activeCount > 0;
                })
                .WithMessage("Cannot deactivate the last active priority. At least one priority must remain active.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketConfigService _configService;

        public Handler(IAppDbContext db, ITicketConfigService configService)
        {
            _db = db;
            _configService = configService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var priority = await _db.TicketPriorityConfigs
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (priority == null)
            {
                throw new NotFoundException("Priority", request.Id);
            }

            priority.IsActive = request.IsActive;

            // If deactivating the default, set another as default
            if (!request.IsActive && priority.IsDefault)
            {
                priority.IsDefault = false;
                var newDefault = await _db.TicketPriorityConfigs
                    .Where(p => p.IsActive && p.Id != priority.Id)
                    .OrderBy(p => p.SortOrder)
                    .FirstOrDefaultAsync(cancellationToken);

                if (newDefault != null)
                {
                    newDefault.IsDefault = true;
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(priority.Id);
        }
    }
}

