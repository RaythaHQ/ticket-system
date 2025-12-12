using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using System.Text.Json;

namespace App.Application.SlaRules.Commands;

public class CreateSlaRule
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public Dictionary<string, object> Conditions { get; init; } = new();
        public int TargetResolutionMinutes { get; init; }
        public int? TargetCloseMinutes { get; init; }
        public bool BusinessHoursEnabled { get; init; }
        public BusinessHoursConfig? BusinessHoursConfig { get; init; }
        public int Priority { get; init; }
        public BreachBehavior? BreachBehavior { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TargetResolutionMinutes).GreaterThan(0);
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // SLA configuration requires CanManageTickets permission
            _permissionService.RequireCanManageTickets();

            var rule = new SlaRule
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ConditionsJson = request.Conditions.Any() ? JsonSerializer.Serialize(request.Conditions) : null,
                TargetResolutionMinutes = request.TargetResolutionMinutes,
                TargetCloseMinutes = request.TargetCloseMinutes,
                BusinessHoursEnabled = request.BusinessHoursEnabled,
                BusinessHoursConfigJson = request.BusinessHoursConfig != null ? JsonSerializer.Serialize(request.BusinessHoursConfig) : null,
                Priority = request.Priority,
                BreachBehaviorJson = request.BreachBehavior != null ? JsonSerializer.Serialize(request.BreachBehavior) : null,
                IsActive = true
            };

            _db.SlaRules.Add(rule);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(rule.Id);
        }
    }
}

