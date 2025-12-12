using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Application.TicketViews.Commands;

public class UpdateTicketView
{
    public record Command : LoggableEntityRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public bool IsDefault { get; init; }
        public ViewConditions? Conditions { get; init; }
        public string? SortPrimaryField { get; init; }
        public string? SortPrimaryDirection { get; init; }
        public string? SortSecondaryField { get; init; }
        public string? SortSecondaryDirection { get; init; }
        public List<string> VisibleColumns { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.VisibleColumns).NotEmpty().WithMessage("At least one visible column is required.");
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

            // Cannot edit system views
            if (view.IsSystem)
                throw new ForbiddenAccessException("Cannot modify system views.");

            // Can only edit own views
            if (view.OwnerStaffId != _currentUser.UserId?.Guid)
                throw new ForbiddenAccessException("Cannot modify views you do not own.");

            view.Name = request.Name;
            view.IsDefault = request.IsDefault;
            view.ConditionsJson = request.Conditions != null ? JsonSerializer.Serialize(request.Conditions) : null;
            view.SortPrimaryField = request.SortPrimaryField;
            view.SortPrimaryDirection = request.SortPrimaryDirection;
            view.SortSecondaryField = request.SortSecondaryField;
            view.SortSecondaryDirection = request.SortSecondaryDirection;
            view.VisibleColumns = request.VisibleColumns;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(view.Id);
        }
    }
}

