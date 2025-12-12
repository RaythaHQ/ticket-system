using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SlaRules.Commands;

public class ReorderSlaRules
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        /// <summary>
        /// List of SLA Rule IDs in the desired priority order (first = highest priority).
        /// </summary>
        public List<Guid> OrderedRuleIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.OrderedRuleIds)
                .NotEmpty()
                .WithMessage("At least one rule ID is required.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ITicketPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            _permissionService.RequireCanManageTickets();

            var rules = await _db.SlaRules
                .Where(r => request.OrderedRuleIds.Contains(r.Id))
                .ToListAsync(cancellationToken);

            if (rules.Count != request.OrderedRuleIds.Count)
            {
                throw new NotFoundException("SlaRules", "One or more rule IDs not found");
            }

            // Update priority based on position in the ordered list
            for (int i = 0; i < request.OrderedRuleIds.Count; i++)
            {
                var ruleId = request.OrderedRuleIds[i];
                var rule = rules.First(r => r.Id == ruleId);
                rule.Priority = i + 1; // Priority starts at 1
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}

