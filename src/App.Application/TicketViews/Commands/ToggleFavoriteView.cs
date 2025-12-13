using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketViews.Commands;

public class ToggleFavoriteView
{
    public record Command : IRequest<CommandResponseDto<bool>>
    {
        public ShortGuid ViewId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.ViewId)
                .NotEmpty()
                .WithMessage("View ID is required.");

            RuleFor(x => x)
                .CustomAsync(async (request, context, cancellationToken) =>
                {
                    var view = await db.TicketViews
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Id == request.ViewId.Guid, cancellationToken);

                    if (view == null)
                    {
                        context.AddFailure("ViewId", "View not found.");
                    }
                });
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId!.Value.Guid;

            var existingFavorite = await _db.UserFavoriteViews
                .FirstOrDefaultAsync(f => f.UserId == userId && f.TicketViewId == request.ViewId.Guid, cancellationToken);

            bool isFavorited;

            if (existingFavorite != null)
            {
                // Unfavorite
                _db.UserFavoriteViews.Remove(existingFavorite);
                isFavorited = false;
            }
            else
            {
                // Favorite - add to the end
                var maxOrder = await _db.UserFavoriteViews
                    .Where(f => f.UserId == userId)
                    .MaxAsync(f => (int?)f.DisplayOrder, cancellationToken) ?? 0;

                var favorite = new UserFavoriteView
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TicketViewId = request.ViewId.Guid,
                    DisplayOrder = maxOrder + 1
                };

                _db.UserFavoriteViews.Add(favorite);
                isFavorited = true;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(isFavorited);
        }
    }
}

