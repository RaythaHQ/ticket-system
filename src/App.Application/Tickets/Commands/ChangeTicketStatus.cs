using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class ChangeTicketStatus
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long Id { get; init; }
        public string NewStatus { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).GreaterThan(0);

            // Validate status against configured active statuses
            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .MustAsync(
                    async (status, cancellationToken) =>
                    {
                        return await db.TicketStatusConfigs.AnyAsync(
                            s => s.DeveloperName == status.ToLower() && s.IsActive,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("Invalid or inactive status value.");
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;
        private readonly ITicketConfigService _configService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ITicketPermissionService permissionService,
            ITicketConfigService configService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
            _configService = configService;
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

            // Check permission - user can change status if they have CanManageTickets, are assigned, or are in the team
            await _permissionService.RequireCanEditTicketAsync(
                ticket.AssigneeId,
                ticket.OwningTeamId,
                cancellationToken
            );

            var oldStatus = ticket.Status;
            var newStatusLower = request.NewStatus.ToLower();
            if (oldStatus == newStatusLower)
            {
                return new CommandResponseDto<long>(ticket.Id);
            }

            ticket.Status = newStatusLower;

            // Get status configs to determine status types
            var newStatusConfig = await _configService.GetStatusByDeveloperNameAsync(
                newStatusLower,
                cancellationToken
            );
            var oldStatusConfig = await _configService.GetStatusByDeveloperNameAsync(
                oldStatus,
                cancellationToken
            );

            // Handle resolved/closed timestamps based on status type
            if (newStatusConfig?.IsClosedType == true)
            {
                // Moving to a closed status
                if (ticket.ResolvedAt == null)
                {
                    ticket.ResolvedAt = DateTime.UtcNow;
                }
                ticket.ClosedAt = DateTime.UtcNow;
            }
            else if (oldStatusConfig?.IsClosedType == true && newStatusConfig?.IsOpenType == true)
            {
                // Reopening from closed to open
                ticket.ResolvedAt = null;
                ticket.ClosedAt = null;
            }

            // Add change log entry
            var changes = new Dictionary<string, object>
            {
                ["Status"] = new { OldValue = oldStatus, NewValue = newStatusLower },
            };

            var oldLabel = oldStatusConfig?.Label ?? oldStatus;
            var newLabel = newStatusConfig?.Label ?? newStatusLower;

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = ticket.Id,
                ActorStaffId = _currentUser.UserId?.Guid,
                FieldChangesJson = JsonSerializer.Serialize(changes),
                Message = $"Status changed from {oldLabel} to {newLabel}",
            };
            ticket.ChangeLogEntries.Add(changeLog);

            ticket.AddDomainEvent(
                new TicketStatusChangedEvent(
                    ticket,
                    oldStatus,
                    newStatusLower,
                    _currentUser.UserId?.Guid
                )
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(ticket.Id);
        }
    }
}
