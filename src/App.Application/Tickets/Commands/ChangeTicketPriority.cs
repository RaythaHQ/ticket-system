using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class ChangeTicketPriority
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public string NewPriority { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).GreaterThan(0);

            // Validate priority against configured active priorities
            RuleFor(x => x.NewPriority)
                .NotEmpty()
                .MustAsync(async (priority, cancellationToken) =>
                {
                    return await db.TicketPriorityConfigs
                        .AnyAsync(p => p.DeveloperName == priority.ToLower() && p.IsActive, cancellationToken);
                })
                .WithMessage("Invalid or inactive priority value.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;
        private readonly ISlaService _slaService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ITicketPermissionService permissionService,
            ISlaService slaService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _slaService = slaService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var ticket = await _db.Tickets.FirstOrDefaultAsync(
                t => t.Id == request.Id,
                cancellationToken
            );

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Check permission - user can edit if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );

            var oldPriority = ticket.Priority;
            if (oldPriority == request.NewPriority)
            {
                return new CommandResponseDto<long>(ticket.Id);
            }

            ticket.Priority = request.NewPriority;

            // Add change log entry
            var changes = new Dictionary<string, object>
            {
                ["Priority"] = new { OldValue = oldPriority, NewValue = request.NewPriority },
            };

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserIdAsGuid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message =
                    $"Priority changed from {TicketPriority.From(oldPriority).Label} to {TicketPriority.From(request.NewPriority).Label}",
            };
            ticket.ChangeLogEntries.Add(changeLog);

            // Re-evaluate SLA since priority affects it
            var previousSlaId = ticket.SlaRuleId;
            await _slaService.EvaluateAndAssignSlaAsync(ticket, cancellationToken);

            if (ticket.SlaRuleId != previousSlaId)
            {
                var slaChangeLog = new TicketChangeLogEntry
                {
                    TicketId = ticket.Id,
                    ActorStaffId = _currentUser.UserIdAsGuid,
                    Message = ticket.SlaRuleId.HasValue
                        ? $"SLA rule re-evaluated and updated"
                        : $"SLA rule removed after re-evaluation",
                };
                ticket.ChangeLogEntries.Add(slaChangeLog);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

