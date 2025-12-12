using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketViews.Commands;

public class DeleteTicketView
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
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
            var view = await _db.TicketViews
                .FirstOrDefaultAsync(v => v.Id == request.Id.Guid, cancellationToken);

            if (view == null)
                throw new NotFoundException("TicketView", request.Id);

            // Cannot delete system views
            if (view.IsSystem)
                throw new ForbiddenAccessException("Cannot delete system views.");

            // Can only delete own views
            if (view.OwnerStaffId != _currentUser.UserId?.Guid)
                throw new ForbiddenAccessException("Cannot delete views you do not own.");

            _db.TicketViews.Remove(view);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.Id);
        }
    }
}

