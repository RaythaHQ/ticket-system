using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Admins.Commands;

public class SetIsActive
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public bool IsActive { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Custom(
                    (request, context) =>
                    {
                        if (request.Id == currentUser.UserId)
                        {
                            context.AddFailure(
                                Constants.VALIDATION_SUMMARY,
                                "You cannot change the status on your own account."
                            );
                            return;
                        }
                    }
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.Users.FirstOrDefaultAsync(
                p => p.Id == request.Id.Guid && p.IsAdmin,
                cancellationToken
            );
            if (entity == null)
                throw new NotFoundException("Admin", request.Id);

            entity.IsActive = request.IsActive;

            // When deactivating (suspending) an admin, clean up their active responsibilities
            if (!request.IsActive)
            {
                // Delete all API keys for this user (revoke programmatic access)
                var apiKeys = await _db
                    .ApiKeys.Where(k => k.UserId == request.Id.Guid)
                    .ToListAsync(cancellationToken);
                _db.ApiKeys.RemoveRange(apiKeys);

                // Remove from all teams
                var teamMemberships = await _db
                    .TeamMemberships.Where(m => m.StaffAdminId == request.Id.Guid)
                    .ToListAsync(cancellationToken);
                _db.TeamMemberships.RemoveRange(teamMemberships);

                // Unfollow all tickets
                var ticketFollowers = await _db
                    .TicketFollowers.Where(f => f.StaffAdminId == request.Id.Guid)
                    .ToListAsync(cancellationToken);
                _db.TicketFollowers.RemoveRange(ticketFollowers);

                // Unassign from all open tickets (set AssigneeId to null)
                // Only unassign from tickets that are not closed/resolved
                var openTickets = await _db
                    .Tickets.Where(t =>
                        t.AssigneeId == request.Id.Guid
                        && t.Status != TicketStatus.CLOSED
                        && t.Status != TicketStatus.RESOLVED
                    )
                    .ToListAsync(cancellationToken);

                foreach (var ticket in openTickets)
                {
                    ticket.AssigneeId = null;
                    ticket.AssignedAt = null;

                    // Add change log entry for the unassignment
                    var changeLog = new TicketChangeLogEntry
                    {
                        TicketId = ticket.Id,
                        ActorStaffId = _currentUser.UserId?.Guid,
                        Message =
                            $"Assignee removed (admin account {entity.FullName} was deactivated)",
                    };
                    ticket.ChangeLogEntries.Add(changeLog);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}
