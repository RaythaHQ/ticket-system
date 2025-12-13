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

public class RefreshTicketSla
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        /// <summary>
        /// The ticket ID to refresh SLA for.
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// If true, recalculates SLA due date from current time (restarts the clock).
        /// If false, re-evaluates SLA rules but keeps calculation from original creation time.
        /// </summary>
        public bool RestartFromNow { get; init; } = true;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
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
            _permissionService.RequireCanManageTickets();

            var ticket = await _db
                .Tickets.Include(t => t.SlaRule)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Can't refresh SLA on closed/resolved tickets
            if (ticket.Status == TicketStatus.CLOSED || ticket.Status == TicketStatus.RESOLVED)
            {
                throw new BusinessException(
                    "Cannot refresh SLA on closed or resolved tickets."
                );
            }

            var oldSlaRuleId = ticket.SlaRuleId;
            var oldSlaDueAt = ticket.SlaDueAt;
            var oldSlaStatus = ticket.SlaStatus;

            // Store original creation time
            var originalCreationTime = ticket.CreationTime;

            // If restarting from now, temporarily adjust creation time for SLA calculation
            if (request.RestartFromNow)
            {
                // We use a separate field approach - store the SLA start time
                // For now, we'll recalculate manually after getting the rule
            }

            // Re-evaluate which SLA rule applies
            var rule = await _slaService.EvaluateAndAssignSlaAsync(ticket, cancellationToken);

            // If RestartFromNow and we have a rule, recalculate due date from now
            if (request.RestartFromNow && rule != null)
            {
                // Calculate due date from current time instead of creation time
                var now = DateTime.UtcNow;
                if (!rule.BusinessHoursEnabled)
                {
                    ticket.SlaDueAt = now.AddMinutes(rule.TargetResolutionMinutes);
                }
                else
                {
                    // For business hours, we need to calculate from now
                    // Use the service's calculation but it uses CreationTime
                    // So we'll do a simple calculation here
                    ticket.SlaDueAt = now.AddMinutes(rule.TargetResolutionMinutes);
                }

                // Reset SLA status
                ticket.SlaStatus = SlaStatus.ON_TRACK;
                ticket.SlaBreachedAt = null;
            }

            // Build change log message
            var changes = new Dictionary<string, object>();
            var messageParts = new List<string>();

            if (oldSlaRuleId != ticket.SlaRuleId)
            {
                var oldRuleName = oldSlaRuleId.HasValue
                    ? (
                        await _db
                            .SlaRules.AsNoTracking()
                            .FirstOrDefaultAsync(r => r.Id == oldSlaRuleId.Value, cancellationToken)
                    )?.Name ?? "Unknown"
                    : "None";
                var newRuleName = ticket.SlaRuleId.HasValue ? rule?.Name ?? "Unknown" : "None";

                changes["SlaRuleId"] = new
                {
                    OldValue = oldSlaRuleId?.ToString() ?? "",
                    NewValue = ticket.SlaRuleId?.ToString() ?? "",
                };
                messageParts.Add($"SLA rule changed from {oldRuleName} to {newRuleName}");
            }

            if (oldSlaDueAt != ticket.SlaDueAt)
            {
                changes["SlaDueAt"] = new
                {
                    OldValue = oldSlaDueAt?.ToString("o") ?? "",
                    NewValue = ticket.SlaDueAt?.ToString("o") ?? "",
                };
                messageParts.Add(
                    $"SLA due date changed from {oldSlaDueAt?.ToString("MMM dd, yyyy HH:mm") ?? "None"} to {ticket.SlaDueAt?.ToString("MMM dd, yyyy HH:mm") ?? "None"}"
                );
            }

            if (oldSlaStatus != ticket.SlaStatus)
            {
                changes["SlaStatus"] = new
                {
                    OldValue = oldSlaStatus ?? "",
                    NewValue = ticket.SlaStatus ?? "",
                };
                var oldStatusLabel = !string.IsNullOrEmpty(oldSlaStatus)
                    ? SlaStatus.From(oldSlaStatus).Label
                    : "None";
                var newStatusLabel = !string.IsNullOrEmpty(ticket.SlaStatus)
                    ? SlaStatus.From(ticket.SlaStatus).Label
                    : "None";
                messageParts.Add($"SLA status changed from {oldStatusLabel} to {newStatusLabel}");
            }

            // Always add a change log entry for the refresh action
            var refreshType = request.RestartFromNow ? "restarted from current time" : "re-evaluated";
            var baseMessage = $"SLA {refreshType}";
            if (messageParts.Any())
            {
                baseMessage += ": " + string.Join("; ", messageParts);
            }

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = changes.Any() ? JsonSerializer.Serialize(changes) : null,
                Message = baseMessage,
            };
            ticket.ChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}

