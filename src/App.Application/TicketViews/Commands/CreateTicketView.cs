using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using System.Text.Json;

namespace App.Application.TicketViews.Commands;

public class CreateTicketView
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
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
            var view = new TicketView
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                OwnerStaffId = _currentUser.UserId?.Guid,
                IsDefault = request.IsDefault,
                IsSystem = false,
                ConditionsJson = request.Conditions != null ? JsonSerializer.Serialize(request.Conditions) : null,
                SortPrimaryField = request.SortPrimaryField,
                SortPrimaryDirection = request.SortPrimaryDirection,
                SortSecondaryField = request.SortSecondaryField,
                SortSecondaryDirection = request.SortSecondaryDirection,
                VisibleColumns = request.VisibleColumns
            };

            _db.TicketViews.Add(view);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(view.Id);
        }
    }
}

