using System.ComponentModel.DataAnnotations;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for displaying a paginated list of tickets.
/// </summary>
public class Index : BaseStaffPageModel, IHasListView<Index.TicketListItemViewModel>
{
    /// <summary>
    /// Gets or sets the list view model containing paginated ticket data.
    /// </summary>
    public ListViewModel<TicketListItemViewModel> ListView { get; set; } =
        new(Enumerable.Empty<TicketListItemViewModel>(), 0);

    /// <summary>
    /// Available views for the view selector.
    /// </summary>
    public IEnumerable<TicketViewDto> AvailableViews { get; set; } =
        Enumerable.Empty<TicketViewDto>();

    /// <summary>
    /// Currently selected view.
    /// </summary>
    public TicketViewDto? SelectedView { get; set; }

    /// <summary>
    /// Currently selected view ID.
    /// </summary>
    public string? CurrentViewId { get; set; }

    /// <summary>
    /// Built-in view key (for non-database views).
    /// </summary>
    public string? BuiltInView { get; set; }

    /// <summary>
    /// Available assignees for filtering (all individuals from all teams).
    /// </summary>
    public List<AssigneeFilterItem> AvailableAssignees { get; set; } = new();

    /// <summary>
    /// Handles GET requests to display the paginated list of tickets.
    /// </summary>
    public async Task<IActionResult> OnGet(
        string search = "",
        string sortBy = "newest",
        string orderBy = $"CreationTime {SortOrder.DESCENDING}",
        int pageNumber = 1,
        int pageSize = 50,
        string? status = null,
        string? priority = null,
        string? assigneeId = null,
        string? viewId = null,
        string? builtInView = null,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = "Tickets";
        ViewData["ActiveMenu"] = "Tickets";

        // Set active submenu based on built-in view
        ViewData["ActiveSubMenu"] = builtInView switch
        {
            "unassigned" => "Unassigned",
            "my-tickets" => "MyTickets",
            "created-by-me" or "my-opened" => "CreatedByMe",
            "team-tickets" => "TeamTickets",
            "all" or null or "" => "AllTickets",
            _ => null,
        };

        // Load available views
        var viewsResponse = await Mediator.Send(new GetTicketViews.Query(), cancellationToken);
        AvailableViews = viewsResponse.Result;

        CurrentViewId = viewId;
        BuiltInView = builtInView;

        // Load assignee filter options (filtered based on built-in view)
        var assigneeOptionsResponse = await Mediator.Send(
            new GetAssigneeFilterOptions.Query
            {
                BuiltInView = builtInView,
                CurrentUserId = CurrentUser.UserId?.Guid,
            },
            cancellationToken
        );
        AvailableAssignees = assigneeOptionsResponse
            .Result.Select(a => new AssigneeFilterItem
            {
                Value = a.Value,
                DisplayText = a.DisplayText,
            })
            .ToList();

        // Map sortBy to orderBy
        var mappedOrderBy = MapSortByToOrderBy(sortBy);
        if (!string.IsNullOrEmpty(mappedOrderBy))
        {
            orderBy = mappedOrderBy;
        }

        // Parse assigneeId if provided (format: "team:guid" or "team:guid:assignee:guid" or "unassigned")
        ShortGuid? parsedAssigneeId = null;
        ShortGuid? parsedTeamId = null;
        bool? unassigned = null;

        if (!string.IsNullOrEmpty(assigneeId))
        {
            if (assigneeId == "unassigned")
            {
                // Unassigned means no team and no individual
                unassigned = true;
            }
            else if (assigneeId.StartsWith("team:"))
            {
                var parts = assigneeId.Split(':');
                if (parts.Length >= 2 && ShortGuid.TryParse(parts[1], out ShortGuid teamGuid))
                {
                    parsedTeamId = teamGuid;

                    // Check if there's an assignee part
                    if (
                        parts.Length >= 4
                        && parts[2] == "assignee"
                        && ShortGuid.TryParse(parts[3], out ShortGuid assigneeGuid)
                    )
                    {
                        // Team and individual assigned
                        parsedAssigneeId = assigneeGuid;
                    }
                    else
                    {
                        // "team:guid" means "Team/Anyone" - show all tickets for this team
                        // (both with and without individual assignees)
                        // Just set TeamId, don't set unassigned
                    }
                }
            }
        }

        var query = new GetTickets.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            Priority = priority,
            AssigneeId = parsedAssigneeId,
            TeamId = parsedTeamId,
            Unassigned = unassigned,
            TeamTickets = builtInView == "team-tickets",
        };

        // Apply view filters
        if (!string.IsNullOrEmpty(viewId))
        {
            var selectedViewResponse = await Mediator.Send(
                new GetTicketViewById.Query { Id = viewId },
                cancellationToken
            );
            SelectedView = selectedViewResponse.Result;

            query = query with { ViewId = viewId };
        }
        else if (!string.IsNullOrEmpty(builtInView))
        {
            // Apply built-in view conditions
            var conditions = await GetBuiltInViewConditionsAsync(builtInView, cancellationToken);
            if (conditions != null)
            {
                query = query with { ViewConditions = conditions };
            }
        }

        var response = await Mediator.Send(query, cancellationToken);

        var items = response.Result.Items.Select(p => new TicketListItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Status = p.Status,
            StatusLabel = p.StatusLabel,
            Priority = p.Priority,
            PriorityLabel = p.PriorityLabel,
            Category = p.Category ?? "-",
            AssigneeName = p.AssigneeName ?? "Unassigned",
            OwningTeamName = p.OwningTeamName ?? "-",
            ContactName = p.ContactName ?? "-",
            ContactId = p.ContactId,
            CommentCount = p.CommentCount,
            SlaDueAt = p.SlaDueAt?.ToString("MMM dd, HH:mm") ?? "-",
            SlaStatusLabel = p.SlaStatusLabel ?? "-",
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
        });

        ListView = new ListViewModel<TicketListItemViewModel>(items, response.Result.TotalCount)
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return Page();
    }

    private Task<ViewConditions?> GetBuiltInViewConditionsAsync(
        string key,
        CancellationToken cancellationToken
    )
    {
        var currentUserId = CurrentUser.UserId?.Guid;

        ViewConditions? result = key switch
        {
            "all" => null,
            "unassigned" => new ViewConditions
            {
                Logic = "AND",
                Filters = new List<ViewFilterCondition>
                {
                    new() { Field = "AssigneeId", Operator = "isnull" },
                    new()
                    {
                        Field = "Status",
                        Operator = "notin",
                        Values = new List<string> { TicketStatus.CLOSED },
                    },
                },
            },
            "my-tickets" => currentUserId.HasValue
                ? new ViewConditions
                {
                    Logic = "AND",
                    Filters = new List<ViewFilterCondition>
                    {
                        new()
                        {
                            Field = "AssigneeId",
                            Operator = "equals",
                            Value = new ShortGuid(currentUserId.Value).ToString(),
                        },
                    },
                }
                : null,
            "my-opened" or "created-by-me" => currentUserId.HasValue
                ? new ViewConditions
                {
                    Logic = "AND",
                    Filters = new List<ViewFilterCondition>
                    {
                        new()
                        {
                            Field = "CreatedByStaffId",
                            Operator = "equals",
                            Value = new ShortGuid(currentUserId.Value).ToString(),
                        },
                    },
                }
                : null,
            // team-tickets is handled via TeamTickets flag in the query
            "team-tickets" => null,
            TicketStatus.OPEN => new ViewConditions
            {
                Logic = "AND",
                Filters = new List<ViewFilterCondition>
                {
                    new()
                    {
                        Field = "Status",
                        Operator = "equals",
                        Value = TicketStatus.OPEN,
                    },
                },
            },
            "recently-closed" => new ViewConditions
            {
                Logic = "AND",
                Filters = new List<ViewFilterCondition>
                {
                    new()
                    {
                        Field = "Status",
                        Operator = "equals",
                        Value = TicketStatus.CLOSED,
                    },
                },
            },
            _ => null,
        };

        return Task.FromResult(result);
    }

    private string? MapSortByToOrderBy(string sortBy)
    {
        return sortBy?.ToLower() switch
        {
            "newest" => $"CreationTime {SortOrder.DESCENDING}",
            "oldest" => $"CreationTime {SortOrder.ASCENDING}",
            "priority" => $"Priority {SortOrder.DESCENDING}, CreationTime {SortOrder.DESCENDING}",
            "status" => $"Status {SortOrder.ASCENDING}, CreationTime {SortOrder.DESCENDING}",
            "assignee" =>
                $"OwningTeamName {SortOrder.ASCENDING}, AssigneeName {SortOrder.ASCENDING}, CreationTime {SortOrder.DESCENDING}",
            "sla" => $"SlaDueAt {SortOrder.ASCENDING}, CreationTime {SortOrder.DESCENDING}",
            _ => null,
        };
    }

    /// <summary>
    /// View model for a single ticket in the list.
    /// </summary>
    public record TicketListItemViewModel
    {
        public long Id { get; init; }

        [Display(Name = "Title")]
        public string Title { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        [Display(Name = "Status")]
        public string StatusLabel { get; init; } = string.Empty;

        public string Priority { get; init; } = string.Empty;

        [Display(Name = "Priority")]
        public string PriorityLabel { get; init; } = string.Empty;

        [Display(Name = "Category")]
        public string Category { get; init; } = string.Empty;

        [Display(Name = "Assignee")]
        public string AssigneeName { get; init; } = string.Empty;

        [Display(Name = "Team")]
        public string OwningTeamName { get; init; } = string.Empty;

        [Display(Name = "Contact")]
        public string ContactName { get; init; } = string.Empty;

        public long? ContactId { get; init; }

        public int CommentCount { get; init; }

        [Display(Name = "SLA Due")]
        public string SlaDueAt { get; init; } = string.Empty;

        [Display(Name = "SLA Status")]
        public string SlaStatusLabel { get; init; } = string.Empty;

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;
    }

    /// <summary>
    /// Item for assignee filter dropdown.
    /// </summary>
    public record AssigneeFilterItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
    }
}
