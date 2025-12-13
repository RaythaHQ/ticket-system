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
        
        /// <summary>
        /// Multi-level sort configuration. Takes precedence over SortPrimaryField/SortSecondaryField.
        /// </summary>
        public List<SortLevelInput>? SortLevels { get; init; }
        
        public List<string> VisibleColumns { get; init; } = new();
        public List<string>? Columns { get; init; }
    }

    public record FilterCondition
    {
        public string Field { get; init; } = string.Empty;
        public string Operator { get; init; } = "eq";
        public string? Value { get; init; }
        public List<string>? Values { get; init; }
        public string? DateType { get; init; }
        public string? RelativeDatePreset { get; init; }
        public int? RelativeDateValue { get; init; }
    }

    public record SortLevelInput
    {
        public int Order { get; init; }
        public string Field { get; init; } = null!;
        public string Direction { get; init; } = "asc";
    }

    public class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidLogicValues = { "AND", "OR" };

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.VisibleColumns).Must(cols => cols == null || cols.Count <= 20).WithMessage("Maximum 20 columns allowed.");
            
            // Validate conditions
            When(x => x.Conditions != null, () =>
            {
                RuleFor(x => x.Conditions!.Logic)
                    .Must(l => ValidLogicValues.Contains(l))
                    .WithMessage("Logic must be 'AND' or 'OR'.");
                    
                RuleFor(x => x.Conditions!.Filters)
                    .Must(f => f == null || f.Count <= 20)
                    .WithMessage("Maximum 20 filter conditions allowed.");
            });
            
            // Validate sort levels
            When(x => x.SortLevels != null && x.SortLevels.Any(), () =>
            {
                RuleFor(x => x.SortLevels!)
                    .Must(s => s.Count <= 6)
                    .WithMessage("Maximum 6 sort levels allowed.");
                    
                RuleForEach(x => x.SortLevels!)
                    .Must(s => s.Direction == "asc" || s.Direction == "desc")
                    .WithMessage("Sort direction must be 'asc' or 'desc'.");
            });
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
                        Value = f.Value,
                        Values = f.Values,
                        DateType = f.DateType,
                        RelativeDatePreset = f.RelativeDatePreset,
                        RelativeDateValue = f.RelativeDateValue
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

            // Set multi-level sort if provided
            if (request.SortLevels?.Any() == true)
            {
                view.SortLevels = request.SortLevels
                    .OrderBy(s => s.Order)
                    .Select(s => new Domain.Entities.ViewSortLevel
                    {
                        Order = s.Order,
                        Field = s.Field,
                        Direction = s.Direction
                    }).ToList();
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(view.Id);
        }
    }
}

