using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Application.SlaRules.Commands;

public class UpdateSlaRule
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
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
        public bool IsActive { get; init; }
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
            _permissionService.RequireCanManageTickets();

            var rule = await _db.SlaRules
                .FirstOrDefaultAsync(r => r.Id == request.Id.Guid, cancellationToken);

            if (rule == null)
                throw new NotFoundException("SlaRule", request.Id);

            rule.Name = request.Name;
            rule.Description = request.Description;
            rule.ConditionsJson = request.Conditions.Any() ? JsonSerializer.Serialize(request.Conditions) : null;
            rule.TargetResolutionMinutes = request.TargetResolutionMinutes;
            rule.TargetCloseMinutes = request.TargetCloseMinutes;
            rule.BusinessHoursEnabled = request.BusinessHoursEnabled;
            rule.BusinessHoursConfigJson = request.BusinessHoursConfig != null ? JsonSerializer.Serialize(request.BusinessHoursConfig) : null;
            rule.Priority = request.Priority;
            rule.BreachBehaviorJson = request.BreachBehavior != null ? JsonSerializer.Serialize(request.BreachBehavior) : null;
            rule.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(rule.Id);
        }
    }
}

