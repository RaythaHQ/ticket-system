using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class UpdateTicketPriority
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public string Label { get; init; } = null!;
        public string ColorName { get; init; } = "secondary";
        public bool IsDefault { get; init; }
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
            var priority = await _db.TicketPriorityConfigs
                .FirstOrDefaultAsync(p => p.Id == request.Id.Guid, cancellationToken);

            if (priority == null)
            {
                throw new NotFoundException("Priority", request.Id);
            }

            // If this is set as default, unset other defaults
            if (request.IsDefault && !priority.IsDefault)
            {
                var currentDefaults = await _db.TicketPriorityConfigs
                    .Where(p => p.IsDefault && p.Id != priority.Id)
                    .ToListAsync(cancellationToken);

                foreach (var p in currentDefaults)
                {
                    p.IsDefault = false;
                }
            }

            priority.Label = request.Label;
            priority.ColorName = request.ColorName.ToLower();
            priority.IsDefault = request.IsDefault;

            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(priority.Id);
        }
    }
}

