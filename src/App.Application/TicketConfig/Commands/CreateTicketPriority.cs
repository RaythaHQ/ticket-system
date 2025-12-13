using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Commands;

public class CreateTicketPriority
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Label { get; init; } = null!;
        public string DeveloperName { get; init; } = null!;
        public string ColorName { get; init; } = "secondary";
        public bool IsDefault { get; init; }
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
                    return !await db.TicketPriorityConfigs
                        .AnyAsync(p => p.DeveloperName == name.ToLower(), cancellationToken);
                })
                .WithMessage("A priority with this developer name already exists.");

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
            // Get the max sort order to add at the end
            var maxSortOrder = await _db.TicketPriorityConfigs
                .MaxAsync(p => (int?)p.SortOrder, cancellationToken) ?? 0;

            // If this is set as default, unset other defaults
            if (request.IsDefault)
            {
                var currentDefaults = await _db.TicketPriorityConfigs
                    .Where(p => p.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (var p in currentDefaults)
                {
                    p.IsDefault = false;
                }
            }

            var priority = new TicketPriorityConfig
            {
                Id = Guid.NewGuid(),
                Label = request.Label,
                DeveloperName = request.DeveloperName.ToLower(),
                ColorName = request.ColorName.ToLower(),
                SortOrder = maxSortOrder + 1,
                IsDefault = request.IsDefault,
                IsBuiltIn = false,
                IsActive = true
            };

            _db.TicketPriorityConfigs.Add(priority);
            await _db.SaveChangesAsync(cancellationToken);

            _configService.InvalidateCache();

            return new CommandResponseDto<ShortGuid>(priority.Id);
        }
    }
}

