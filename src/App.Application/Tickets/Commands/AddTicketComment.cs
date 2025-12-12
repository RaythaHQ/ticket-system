using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Commands;

public class AddTicketComment
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public long TicketId { get; init; }
        public string Body { get; init; } = null!;
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TicketId).GreaterThan(0);
            RuleFor(x => x.Body).NotEmpty();
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
            // Check ticket exists without tracking to avoid modification issues
            var ticketExists = await _db.Tickets
                .AsNoTracking()
                .AnyAsync(t => t.Id == request.TicketId, cancellationToken);

            if (!ticketExists)
                throw new NotFoundException("Ticket", request.TicketId);

            var authorId = _currentUser.UserId?.Guid ?? throw new ForbiddenAccessException("User not authenticated.");

            var comment = new TicketComment
            {
                Id = Guid.NewGuid(),
                TicketId = request.TicketId,
                AuthorStaffId = authorId,
                Body = request.Body
            };

            // Add directly to DbSet instead of navigation property
            _db.TicketComments.Add(comment);

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(comment.Id);
        }
    }
}

