using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class UpdateTicketStatus
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public string Label { get; init; } = null!;
        public string ColorName { get; init; } = "secondary";
        public string StatusType { get; init; } = TicketStatusType.OPEN;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Label)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ColorName)
                .NotEmpty()
                .Must(c => ValidColors.Contains(c.ToLower()))
                .WithMessage("Color must be one of: primary, secondary, success, danger, warning, info, light, dark");

            RuleFor(x => x.StatusType)
                .NotEmpty()
                .Must(t => t == TicketStatusType.OPEN || t == TicketStatusType.CLOSED)
                .WithMessage("Status type must be 'open' or 'closed'.");
        }

        private static readonly HashSet<string> ValidColors = new()
        {
            "primary", "secondary", "success", "danger", "warning", "info", "light", "dark"
        };
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

            // Validate: if this is the first status (top of list), it must remain Open type
            if (status.SortOrder == 1 && request.StatusType == TicketStatusType.CLOSED)
            {
                throw new BusinessException("The first status in the list must be of type 'Open' as it is used as the default for new tickets.");
            }

            status.Label = request.Label;
            status.ColorName = request.ColorName.ToLower();
            status.StatusType = request.StatusType.ToLower();

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(status.Id);
        }
    }
}

