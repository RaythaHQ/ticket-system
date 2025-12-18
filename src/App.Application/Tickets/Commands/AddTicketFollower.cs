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
/// Adds a user as a follower to a ticket. This allows any user with access to the ticket
/// to add any other user as a follower.
/// </summary>
public class AddTicketFollower
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
            var ticket = await _db
                .Tickets.Include(t => t.Followers)
                .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.TicketId);

            var targetUserId = request.StaffAdminId.Guid;

            // Verify the target user exists
            var targetUser = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);

            if (targetUser == null)
                throw new NotFoundException("User", request.StaffAdminId);

            // Check if already following
            var existingFollow = ticket.Followers.FirstOrDefault(f =>
                f.StaffAdminId == targetUserId
            );
            if (existingFollow != null)
            {
                // Already following, return success (idempotent)
                return new CommandResponseDto<ShortGuid>(existingFollow.Id);
            }

            // Get actor's name for the changelog
            var actorId = _currentUser.UserId?.Guid;
            var actor = actorId.HasValue
                ? await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == actorId.Value, cancellationToken)
                : null;

            var follower = new TicketFollower
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                StaffAdminId = targetUserId,
            };

            _db.TicketFollowers.Add(follower);

            // Add change log entry
            var message =
                actorId.HasValue && actorId.Value == targetUserId
                    ? $"{targetUser.FullName} started following this ticket"
                    : $"{actor?.FullName ?? "Unknown"} added {targetUser.FullName} as a follower";

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
