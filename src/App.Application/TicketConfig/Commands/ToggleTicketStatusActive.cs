using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class ToggleTicketStatusActive
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
            // Cannot deactivate if it's the only active status
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (cmd.IsActive) return true; // Activating is always allowed

                    var activeCount = await db.TicketStatusConfigs
                        .CountAsync(s => s.IsActive && s.Id != cmd.Id.Guid, cancellationToken);

                    return activeCount > 0;
                })
                .WithMessage("Cannot deactivate the last active status. At least one status must remain active.");

            // Cannot deactivate if it would leave no Open-type statuses
            RuleFor(x => x)
                .MustAsync(async (cmd, cancellationToken) =>
                {
                    if (cmd.IsActive) return true;

                    var status = await db.TicketStatusConfigs
                        .FirstOrDefaultAsync(s => s.Id == cmd.Id.Guid, cancellationToken);

                    if (status == null || status.StatusType != TicketStatusType.OPEN)
                        return true;

                    var openCount = await db.TicketStatusConfigs
                        .CountAsync(s => s.IsActive && s.StatusType == TicketStatusType.OPEN && s.Id != cmd.Id.Guid, cancellationToken);

                    return openCount > 0;
                })
                .WithMessage("Cannot deactivate the last Open-type status. At least one Open status must remain active for new tickets.");
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
            var status = await _db.TicketStatusConfigs
                .FirstOrDefaultAsync(s => s.Id == request.Id.Guid, cancellationToken);

            if (status == null)
            {
                throw new NotFoundException("Status", request.Id);
            }

            // Cannot deactivate the first status (default for new tickets)
            if (!request.IsActive && status.SortOrder == 1)
            {
                throw new BusinessException("Cannot deactivate the default status (first in the list). Reorder statuses first to change the default.");
            }

            status.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(status.Id);
        }
    }
}

