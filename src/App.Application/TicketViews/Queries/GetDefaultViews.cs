using App.Application.Common.Models;
using Mediator;

namespace App.Application.TicketViews.Queries;

/// <summary>
/// Returns the default system views that should be seeded.
/// </summary>
public class GetDefaultViews
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<DefaultViewDefinition>>>;

    public class Handler
        : IRequestHandler<Query, IQueryResponseDto<IEnumerable<DefaultViewDefinition>>>
    {
        private static readonly List<string> DefaultColumns = new()
        {
            "Id",
            "Title",
            "Status",
            "Priority",
            "Category",
            "AssigneeName",
            "TeamName",
            "CreationTime",
        };

        public ValueTask<IQueryResponseDto<IEnumerable<DefaultViewDefinition>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var views = new List<DefaultViewDefinition>
            {
                new()
                {
                    Name = "All Tickets",
                    Key = "all",
                    Conditions = null,
                    VisibleColumns = DefaultColumns,
                },
                new()
                {
                    Name = "My Active Tickets",
                    Key = "my-active",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "AssigneeId", Operator = "isnotnull" },
                            new()
                            {
                                Field = "StatusType",
                                Operator = "equals",
                                Value = "open",
                            },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                    RequiresCurrentUser = true,
                    CurrentUserField = "AssigneeId",
                },
                new()
                {
                    Name = "My Tickets",
                    Key = "my-tickets",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "AssigneeId", Operator = "isnotnull" },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                    RequiresCurrentUser = true,
                    CurrentUserField = "AssigneeId",
                },
                new()
                {
                    Name = "Tickets I Opened",
                    Key = "my-opened",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "CreatedByStaffId", Operator = "isnotnull" },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                    RequiresCurrentUser = true,
                    CurrentUserField = "CreatedByStaffId",
                },
                new()
                {
                    Name = "Created by Me",
                    Key = "created-by-me",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "CreatedByStaffId", Operator = "isnotnull" },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                    RequiresCurrentUser = true,
                    CurrentUserField = "CreatedByStaffId",
                },
                new()
                {
                    Name = "Team Tickets",
                    Key = "team-tickets",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "OwningTeamId", Operator = "isnotnull" },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                    RequiresCurrentUser = true,
                    CurrentUserField = "OwningTeamId",
                },
                new()
                {
                    Name = "Unassigned Tickets",
                    Key = "unassigned",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new() { Field = "AssigneeId", Operator = "isnull" },
                            new()
                            {
                                Field = "StatusType",
                                Operator = "equals",
                                Value = "open",
                            },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                },
                new()
                {
                    Name = "Open Tickets",
                    Key = "open",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new()
                            {
                                Field = "StatusType",
                                Operator = "equals",
                                Value = "open",
                            },
                        },
                    },
                    VisibleColumns = DefaultColumns,
                },
                new()
                {
                    Name = "Recently Updated",
                    Key = "recently-updated",
                    Conditions = null,
                    SortPrimaryField = "LastModificationTime",
                    SortPrimaryDirection = "DESC",
                    VisibleColumns = new List<string>
                    {
                        "Id",
                        "Title",
                        "Status",
                        "Priority",
                        "AssigneeName",
                        "LastModificationTime",
                    },
                },
                new()
                {
                    Name = "Recently Closed",
                    Key = "recently-closed",
                    Conditions = new ViewConditions
                    {
                        Logic = "AND",
                        Filters = new List<ViewFilterCondition>
                        {
                            new()
                            {
                                Field = "StatusType",
                                Operator = "equals",
                                Value = "closed",
                            },
                        },
                    },
                    SortPrimaryField = "ClosedAt",
                    SortPrimaryDirection = "DESC",
                    VisibleColumns = new List<string>
                    {
                        "Id",
                        "Title",
                        "Priority",
                        "AssigneeName",
                        "ClosedAt",
                    },
                },
            };

            return ValueTask.FromResult<IQueryResponseDto<IEnumerable<DefaultViewDefinition>>>(
                new QueryResponseDto<IEnumerable<DefaultViewDefinition>>(views)
            );
        }
    }
}

public record DefaultViewDefinition
{
    public string Name { get; init; } = null!;
    public string Key { get; init; } = null!;
    public ViewConditions? Conditions { get; init; }
    public string? SortPrimaryField { get; init; }
    public string? SortPrimaryDirection { get; init; }
    public List<string> VisibleColumns { get; init; } = new();
    public bool RequiresCurrentUser { get; init; }
    public string? CurrentUserField { get; init; }
}
