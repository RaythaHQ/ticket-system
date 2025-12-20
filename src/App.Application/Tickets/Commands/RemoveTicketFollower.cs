using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

/// <summary>
/// Removes a user from a ticket's followers. This allows any user with access to the ticket
/// to remove any follower.
/// </summary>
public class RemoveTicketFollower
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long TicketId { get; init; }
        public ShortGuid StaffAdminId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
            RuleFor(x => x.StaffAdminId).NotEmpty();
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
            var ticket = await _db.Tickets.FirstOrDefaultAsync(
                t => t.Id == request.TicketId,
                cancellationToken
            );

            if (ticket == null)
                throw new NotFoundException("Ticket", request.TicketId);

            var targetUserId = request.StaffAdminId.Guid;

            var follower = await _db.TicketFollowers.FirstOrDefaultAsync(
                f => f.TicketId == request.TicketId && f.StaffAdminId == targetUserId,
                cancellationToken
            );

            if (follower == null)
            {
                // Not following, return success (idempotent)
                return new CommandResponseDto<ShortGuid>(Guid.Empty);
            }

            // Get target user's name for the changelog
            var targetUser = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

            // Get actor's name for the changelog
            var actorId = _currentUser.UserIdAsGuid;
            var actor = actorId.HasValue
                ? await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == actorId.Value, cancellationToken)
                : null;

            _db.TicketFollowers.Remove(follower);

            // Add change log entry
            var targetName = targetUser?.FullName ?? "Unknown";
            var message =
                actorId.HasValue && actorId.Value == targetUserId
                    ? $"{targetName} stopped following this ticket"
                    : $"{actor?.FullName ?? "Unknown"} removed {targetName} as a follower";

            var changeLog = new TicketChangeLogEntry
            {
                TicketId = request.TicketId,
                ActorStaffId = actorId,
                Message = message,
            };
            _db.TicketChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(follower.Id);
        }
    }
}
