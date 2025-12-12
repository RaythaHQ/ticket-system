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
        public string? Description { get; init; }
        public bool IsDefault { get; init; }
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
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ITicketPermissionService _permissionService;

        public Handler(IAppDbContext db, ICurrentUser currentUser, ITicketPermissionService permissionService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
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

            // System views can only be edited by users with ManageSystemViews permission
            if (view.IsSystem)
            {
                _permissionService.RequireCanManageSystemViews();
            }
            else
            {
                // Can only edit own views
                if (view.OwnerStaffId != _currentUser.UserId?.Guid)
                    throw new ForbiddenAccessException("Cannot modify views you do not own.");
            }

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

            view.Name = request.Name;
            view.Description = request.Description;
            view.IsDefault = request.IsDefault;
            view.ConditionsJson = conditionsJson;
            view.SortPrimaryField = sortField;
            view.SortPrimaryDirection = sortDirection;
            view.SortSecondaryField = request.SortSecondaryField;
            view.SortSecondaryDirection = request.SortSecondaryDirection;
            view.VisibleColumns = columns;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(view.Id);
        }
    }
}

