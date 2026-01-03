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

public class ExtendTicketSla
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        /// <summary>
        /// The ticket ID to extend SLA for.
        /// </summary>
        public long Id { get; init; }

        /// <summary>
        /// Number of hours to extend the SLA by.
        /// Must be positive (> 0).
        /// </summary>
        public int ExtensionHours { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
            RuleFor(x => x.ExtensionHours)
                .GreaterThan(0)
                .WithMessage("Extension hours must be greater than zero.");
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
            var ticket = await _db
                .Tickets.Include(t => t.SlaRule)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            // Can't extend SLA on closed/resolved tickets
            if (ticket.Status == TicketStatus.CLOSED || ticket.Status == TicketStatus.RESOLVED)
            {
                throw new BusinessException("Cannot extend SLA on closed or resolved tickets.");
            }

            // Check if user can edit this ticket
            var canEdit = await _permissionService.CanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );
            if (!canEdit)
            {
                throw new ForbiddenAccessException(
                    "You do not have permission to extend SLA for this ticket."
                );
            }

            // Check extension limits for non-privileged users
            var hasUnlimitedExtensions = _permissionService.CanManageTickets();
            if (!hasUnlimitedExtensions)
            {
                var settings = SlaExtensionSettings.FromEnvironment();

                // Check extension count limit
                if (ticket.SlaExtensionCount >= settings.MaxExtensions)
                {
                    throw new ForbiddenAccessException(
                        $"Maximum extensions ({settings.MaxExtensions}) reached. Contact a manager to extend further."
                    );
                }

                // Check max hours limit
                if (request.ExtensionHours > settings.MaxExtensionHours)
                {
                    throw new BusinessException(
                        $"Extension cannot exceed {settings.MaxExtensionHours} hours. You requested {request.ExtensionHours} hours."
                    );
                }
            }

            // Store old values for change log
            var oldSlaDueAt = ticket.SlaDueAt;
            var oldSlaStatus = ticket.SlaStatus;

            // Calculate new due date
            var newSlaDueAt = _slaService.CalculateExtendedDueDate(
                ticket.SlaDueAt,
                request.ExtensionHours
            );

            // Validate the new due date is in the future
            if (newSlaDueAt <= DateTime.UtcNow)
            {
                throw new BusinessException("Extension would result in a due date in the past.");
            }

            // Update ticket
            ticket.SlaDueAt = newSlaDueAt;
            ticket.SlaExtensionCount++;

            // If SLA was breached and new due date is in future, update status to ON_TRACK
            if (
                ticket.SlaStatus == SlaStatus.BREACHED
                || ticket.SlaStatus == SlaStatus.APPROACHING_BREACH
            )
            {
                ticket.SlaStatus = SlaStatus.ON_TRACK;
                ticket.SlaBreachedAt = null;
            }

            // If no SLA status was set (ad-hoc extension), set it now
            if (string.IsNullOrEmpty(ticket.SlaStatus))
            {
                ticket.SlaStatus = SlaStatus.ON_TRACK;
            }

            // Build change log entry
            var changes = new Dictionary<string, object>
            {
                ["SlaDueAt"] = new
                {
                    OldValue = oldSlaDueAt?.ToString("o") ?? "",
                    NewValue = ticket.SlaDueAt?.ToString("o") ?? "",
                },
            };

            if (oldSlaStatus != ticket.SlaStatus)
            {
                changes["SlaStatus"] = new
                {
                    OldValue = oldSlaStatus ?? "",
                    NewValue = ticket.SlaStatus ?? "",
                };
            }

            var oldDueDateFormatted = oldSlaDueAt?.ToString("MMM dd, yyyy h:mm tt") ?? "None";
            var newDueDateFormatted = ticket.SlaDueAt?.ToString("MMM dd, yyyy h:mm tt") ?? "None";
            var message =
                $"Extended SLA by {request.ExtensionHours} hour{(request.ExtensionHours != 1 ? "s" : "")}. Due date changed from {oldDueDateFormatted} to {newDueDateFormatted}.";

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserIdAsGuid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = message,
            };
            ticket.ChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
