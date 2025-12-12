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
        public string? Description { get; init; }
        public Guid? OwnerUserId { get; init; }
        public bool IsDefault { get; init; }
        public bool IsSystemView { get; init; }
        public ViewConditions? Conditions { get; init; }
        public List<FilterCondition>? Filters { get; init; }
        public string? SortPrimaryField { get; init; }
        public string? SortField { get; init; }
        public string? SortPrimaryDirection { get; init; }
        public string? SortDirection { get; init; }
        public string? SortSecondaryField { get; init; }
        public string? SortSecondaryDirection { get; init; }
        public List<string> VisibleColumns { get; init; } = new();
        public List<string>? Columns { get; init; }
    }

    public record FilterCondition
    {
        public string Field { get; init; } = string.Empty;
        public string Operator { get; init; } = "equals";
        public string? Value { get; init; }
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
            // Support both Filters and Conditions for backward compat
            string? conditionsJson = null;
            if (request.Conditions != null)
            {
                conditionsJson = JsonSerializer.Serialize(request.Conditions);
            }
            else if (request.Filters?.Any() == true)
            {
                var conditions = new ViewConditions
                {
                    Logic = "AND",
                    Filters = request.Filters.Select(f => new ViewFilterCondition
                    {
                        Field = f.Field,
                        Operator = f.Operator,
                        Value = f.Value
                    }).ToList()
                };
                conditionsJson = JsonSerializer.Serialize(conditions);
            }

            // Support both property naming conventions
            var columns = request.Columns?.Any() == true ? request.Columns : request.VisibleColumns;
            var sortField = request.SortField ?? request.SortPrimaryField;
            var sortDirection = request.SortDirection ?? request.SortPrimaryDirection;

            var view = new TicketView
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OwnerStaffId = request.OwnerUserId ?? _currentUser.UserId?.Guid,
                IsDefault = request.IsDefault,
                IsSystem = request.IsSystemView,
                ConditionsJson = conditionsJson,
                SortPrimaryField = sortField,
                SortPrimaryDirection = sortDirection,
                SortSecondaryField = request.SortSecondaryField,
                SortSecondaryDirection = request.SortSecondaryDirection,
                VisibleColumns = columns
            };

            _db.TicketViews.Add(view);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(view.Id);
        }
    }
}

