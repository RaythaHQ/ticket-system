using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class CreateTicketStatus
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public string ColorName { get; init; } = "secondary";
        public string StatusType { get; init; } = TicketStatusType.OPEN;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Label)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.DeveloperName)
                .NotEmpty()
                .MaximumLength(100)
                .Matches("^[a-z][a-z0-9_]*$")
                .WithMessage("Developer name must start with a lowercase letter and contain only lowercase letters, numbers, and underscores.");

            RuleFor(x => x.DeveloperName)
                .MustAsync(async (name, cancellationToken) =>
                {
                    return !await db.TicketStatusConfigs
                        .AnyAsync(s => s.DeveloperName == name.ToLower(), cancellationToken);
                })
                .WithMessage("A status with this developer name already exists.");

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
            // Get the max sort order to add at the end
            var maxSortOrder = await _db.TicketStatusConfigs
                .MaxAsync(s => (int?)s.SortOrder, cancellationToken) ?? 0;

            var status = new TicketStatusConfig
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToLower(),
                ColorName = request.ColorName.ToLower(),
                SortOrder = maxSortOrder + 1,
                StatusType = request.StatusType.ToLower(),
                IsBuiltIn = false,
                IsActive = true
            };

            _db.TicketStatusConfigs.Add(status);
            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(status.Id);
        }
    }
}

