using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class UnfollowTicket
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long TicketId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
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

            var userId =
                _currentUser.UserId?.Guid
                ?? throw new ForbiddenAccessException("User not authenticated.");

            var follower = await _db.TicketFollowers.FirstOrDefaultAsync(
                f => f.TicketId == request.TicketId && f.StaffAdminId == userId,
                cancellationToken
            );

            if (follower == null)
            {
                // Not following, return success (idempotent)
                return new CommandResponseDto<ShortGuid>(Guid.Empty);
            }

            // Get user's name for the changelog
            var user = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            _db.TicketFollowers.Remove(follower);

            // Add change log entry
            var changeLog = new TicketChangeLogEntry
            {
                TicketId = request.TicketId,
                ActorStaffId = userId,
                Message = $"{user?.FullName ?? "Unknown"} stopped following this ticket",
            };
            _db.TicketChangeLogEntries.Add(changeLog);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(follower.Id);
        }
    }
}
