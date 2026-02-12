using System.ComponentModel.DataAnnotations;
using System.Reflection;
using App.Application.Common.Interfaces;
using App.Application.TicketConfig;
using App.Application.Tickets;
using App.Application.Tickets.Queries;
using App.Application.TicketViews;
using App.Application.TicketViews.Queries;
using App.Domain.ValueObjects;
using App.Web.Areas.Shared.Models;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for displaying a paginated list of tickets.
/// </summary>
public class Index : BaseStaffPageModel, IHasListView<Index.TicketListItemViewModel>
{
    private readonly ITicketConfigService _configService;

    public Index(ITicketConfigService configService)
    {
        _configService = configService;
    }

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
    /// Available statuses for filtering.
    /// </summary>
    public IReadOnlyList<TicketStatusConfigDto> AvailableStatuses { get; set; } =
        new List<TicketStatusConfigDto>();

    /// <summary>
    /// Available priorities for filtering.
    /// </summary>
    public IReadOnlyList<TicketPriorityConfigDto> AvailablePriorities { get; set; } =
        new List<TicketPriorityConfigDto>();

    /// <summary>
    /// Whether the current user can export tickets.
    /// </summary>
    public bool CanExport { get; set; }

    /// <summary>
    /// Column definitions for visible columns in the current view.
    /// </summary>
    public List<ColumnDefinition> VisibleColumnDefinitions { get; set; } = new();

    /// <summary>
    /// Default column fields when no view is selected.
    /// </summary>
    public static readonly List<string> DefaultColumnFields = new()
    {
        "Id",
        "Title",
        "Status",
        "Priority",
        "AssigneeName",
        "ContactId",
        "SlaDueAt",
        "CreationTime",
    };

    /// <summary>
    /// Current sort mode: "view" for view's default sort, or "newest"/"oldest"/etc. for manual override.
    /// </summary>
    public string CurrentSortBy { get; set; } = "newest";

    /// <summary>
    /// Whether we're using the view's custom sort order.
    /// </summary>
    public bool IsUsingViewSort { get; set; }

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
        long? contactId = null,
        string? teamId = null,
        string? createdById = null,
        string? viewId = null,
        string? builtInView = null,
        string? snoozeFilter = null,
        bool showSnoozed = false,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = "Tickets";
        ViewData["ActiveMenu"] = "Tickets";

        // Set active submenu based on built-in view, or active view ID for custom views
        if (!string.IsNullOrEmpty(viewId))
        {
            // Custom view - set the view ID so sidebar can highlight favorited views
            ViewData["ActiveViewId"] = viewId;
            ViewData["ActiveSubMenu"] = null; // Don't highlight any built-in view
        }
        else
        {
            ViewData["ActiveSubMenu"] = builtInView switch
            {
                "unassigned" => "Unassigned",
                "my-tickets" => "MyTickets",
                "created-by-me" or "my-opened" => "CreatedByMe",
                "team-tickets" => "TeamTickets",
                "following" => "Following",
                "overdue" => "Overdue",
                "snoozed" => "Snoozed",
                "all" or null or "" => "AllTickets",
                _ => null,
            };
        }

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

        // Load statuses and priorities for filters
        AvailableStatuses = await _configService.GetAllStatusesAsync(
            includeInactive: true,
            cancellationToken
        );
        AvailablePriorities = await _configService.GetAllPrioritiesAsync(
            includeInactive: true,
            cancellationToken
        );

        // Map sortBy to orderBy
        var mappedOrderBy = MapSortByToOrderBy(sortBy);
        if (!string.IsNullOrEmpty(mappedOrderBy))
        {
            orderBy = mappedOrderBy;
        }

        // Parse teamId if provided directly
        ShortGuid? parsedTeamId = null;
        if (!string.IsNullOrEmpty(teamId) && ShortGuid.TryParse(teamId, out ShortGuid teamGuid))
        {
            parsedTeamId = teamGuid;
        }

        // Parse assigneeId if provided (format: "team:guid" or "team:guid:assignee:guid" or "unassigned" or plain ShortGuid)
        ShortGuid? parsedAssigneeId = null;
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
                if (
                    parts.Length >= 2
                    && ShortGuid.TryParse(parts[1], out ShortGuid assigneeTeamGuid)
                )
                {
                    // If teamId wasn't already set, use the one from assigneeId
                    if (!parsedTeamId.HasValue)
                    {
                        parsedTeamId = assigneeTeamGuid;
                    }

                    // Check if there's an assignee part or unassigned
                    if (parts.Length >= 3 && parts[2] == "unassigned")
                    {
                        // "team:guid:unassigned" means Team/Unassigned - tickets for this team with no individual
                        unassigned = true;
                    }
                    else if (
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
            else if (ShortGuid.TryParse(assigneeId, out ShortGuid plainAssigneeGuid))
            {
                // Plain ShortGuid - direct assignee ID (from dashboard search)
                parsedAssigneeId = plainAssigneeGuid;
            }
        }

        // Parse createdById if provided
        ShortGuid? parsedCreatedById = null;
        if (
            !string.IsNullOrEmpty(createdById)
            && ShortGuid.TryParse(createdById, out ShortGuid createdByGuid)
        )
        {
            parsedCreatedById = createdByGuid;
        }

        // Parse status filter - could be "type:open", "type:closed", or a specific status
        string? statusFilter = null;
        string? statusTypeFilter = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (status.StartsWith("type:"))
            {
                statusTypeFilter = status.Substring(5); // "open" or "closed"
            }
            else
            {
                statusFilter = status;
            }
        }

        var query = new GetTickets.Query
        {
            Search = search,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = statusFilter,
            StatusType = statusTypeFilter,
            Priority = priority,
            AssigneeId = parsedAssigneeId,
            TeamId = parsedTeamId,
            ContactId = contactId,
            CreatedByStaffId = parsedCreatedById,
            Unassigned = unassigned,
            TeamTickets = builtInView == "team-tickets",
            Following = builtInView == "following",
            SortBy = sortBy,
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

            // Handle sort: "view" means use view's default sort, otherwise use the specified sortBy
            if (sortBy == "view" && SelectedView?.SortLevels.Count > 0)
            {
                IsUsingViewSort = true;
                CurrentSortBy = "view";
                query = query with { SortBy = "view" };
            }
            else if (string.IsNullOrEmpty(sortBy) && SelectedView?.SortLevels.Count > 0)
            {
                // Default to view's sort when first loading the view
                IsUsingViewSort = true;
                CurrentSortBy = "view";
                query = query with { SortBy = "view" };
            }
            else
            {
                IsUsingViewSort = false;
                CurrentSortBy = sortBy ?? "newest";
            }
        }
        else if (!string.IsNullOrEmpty(builtInView))
        {
            // Apply built-in view conditions with snooze filter overrides
            var conditions = await GetBuiltInViewConditionsAsync(
                builtInView,
                snoozeFilter,
                showSnoozed,
                cancellationToken
            );
            if (conditions != null)
            {
                query = query with { ViewConditions = conditions };
            }
            CurrentSortBy = sortBy ?? "newest";
        }
        else
        {
            CurrentSortBy = sortBy ?? "newest";
        }

        // Setup visible column definitions
        if (SelectedView?.VisibleColumns.Count > 0)
        {
            VisibleColumnDefinitions = SelectedView
                .VisibleColumns.Select(field => ColumnRegistry.GetByField(field))
                .Where(col => col != null)
                .Cast<ColumnDefinition>()
                .ToList();
        }
        else
        {
            // Default columns
            VisibleColumnDefinitions = DefaultColumnFields
                .Select(field => ColumnRegistry.GetByField(field))
                .Where(col => col != null)
                .Cast<ColumnDefinition>()
                .ToList();
        }

        var response = await Mediator.Send(query, cancellationToken);

        // Get user's team IDs for edit permission check
        var canManageTickets = TicketPermissionService.CanManageTickets();
        var currentUserId = CurrentUser.UserId?.Guid;
        var userTeamIds = await TicketPermissionService.GetUserTeamIdsAsync(cancellationToken);

        var items = response.Result.Items.Select(p => new TicketListItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Status = p.Status,
            StatusLabel = p.StatusLabel,
            Priority = p.Priority,
            PriorityLabel = p.PriorityLabel,
            Language = p.Language,
            LanguageLabel = p.LanguageLabel,
            Category = p.Category ?? "-",
            AssigneeName = p.AssigneeName ?? "Unassigned",
            OwningTeamName = p.OwningTeamName ?? "",
            ContactName = p.ContactName ?? "-",
            ContactId = p.ContactId,
            CommentCount = p.CommentCount,
            CompletedTaskCount = p.CompletedTaskCount,
            TotalTaskCount = p.TotalTaskCount,
            IsSnoozed = p.IsSnoozed,
            SnoozedUntil = p.SnoozedUntil,
            SnoozedUntilFormatted = p.SnoozedUntil.HasValue
                ? CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(p.SnoozedUntil.Value)
                : "",
            IsRecentlyUnsnoozed = p.IsRecentlyUnsnoozed,
            SlaDueAt = p.SlaDueAt.HasValue
                ? CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.SlaDueAt.Value
                )
                : "-",
            SlaStatusLabel = p.SlaStatusLabel ?? "-",
            CreationTime = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                p.CreationTime
            ),
            LastModificationTime = p.LastModificationTime.HasValue
                ? CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.LastModificationTime.Value
                )
                : "",
            ClosedAt = p.ClosedAt.HasValue
                ? CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    p.ClosedAt.Value
                )
                : "",
            Description = p.Description ?? "",
            Tags = p.Tags?.Count > 0 ? string.Join(", ", p.Tags) : "",
            CreatedByName = p.CreatedByStaffName ?? "",
            CanEdit =
                canManageTickets
                || (currentUserId.HasValue && p.AssigneeId == currentUserId.Value)
                || (p.OwningTeamId.HasValue && userTeamIds.Contains(p.OwningTeamId.Value)),
        });

        ListView = new ListViewModel<TicketListItemViewModel>(items, response.Result.TotalCount)
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            BuiltInView = builtInView,
            ViewId = viewId,
            SortBy = sortBy,
            Status = status,
            Priority = priority,
            AssigneeId = assigneeId,
            TeamId = teamId,
            CreatedById = createdById,
            ContactId = contactId,
        };

        // Check if user can export (has ImportExportTickets permission)
        CanExport = CurrentUser.SystemPermissions.Contains(
            Domain.Entities.BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION
        );

        return Page();
    }

    /// <summary>
    /// Handles POST to initiate a CSV export of the current view.
    /// </summary>
    public async Task<IActionResult> OnPostExportAsync(
        string? viewId = null,
        string? builtInView = null,
        string? search = null,
        string? status = null,
        string? priority = null,
        string? sortBy = null,
        CancellationToken cancellationToken = default
    )
    {
        // Get columns from the view if one is selected, otherwise use defaults
        var columns = new List<string>
        {
            "id",
            "title",
            "status",
            "priority",
            "category",
            "contactname",
            "assigneename",
            "teamname",
            "creationtime",
        };

        // If a view is selected, use its visible columns for the export
        if (!string.IsNullOrEmpty(viewId))
        {
            var viewResponse = await Mediator.Send(
                new Application.TicketViews.Queries.GetTicketViewById.Query { Id = viewId },
                cancellationToken
            );

            if (viewResponse.Result?.VisibleColumns?.Any() == true)
            {
                columns = viewResponse.Result.VisibleColumns;
            }
        }

        var filters = new List<Application.Exports.Models.ExportFilter>();
        if (!string.IsNullOrEmpty(status))
        {
            if (status.StartsWith("type:"))
            {
                // Status type filter
                filters.Add(
                    new Application.Exports.Models.ExportFilter
                    {
                        Field = "statustype",
                        Operator = "equals",
                        Value = status.Substring(5), // "open" or "closed"
                    }
                );
            }
            else
            {
                filters.Add(
                    new Application.Exports.Models.ExportFilter
                    {
                        Field = "status",
                        Operator = "equals",
                        Value = status,
                    }
                );
            }
        }
        if (!string.IsNullOrEmpty(priority))
        {
            filters.Add(
                new Application.Exports.Models.ExportFilter
                {
                    Field = "priority",
                    Operator = "equals",
                    Value = priority,
                }
            );
        }

        // Handle built-in view conditions (export doesn't use snooze filter overrides)
        if (!string.IsNullOrEmpty(builtInView))
        {
            var conditions = await GetBuiltInViewConditionsAsync(builtInView, null, false, cancellationToken);
            if (conditions != null)
            {
                foreach (var filter in conditions.Filters)
                {
                    filters.Add(
                        new Application.Exports.Models.ExportFilter
                        {
                            Field = filter.Field,
                            Operator = filter.Operator,
                            Value = filter.Value,
                        }
                    );
                }
            }
        }

        var sortField = "creationtime";
        var sortDirection = "desc";
        switch (sortBy?.ToLower())
        {
            case "oldest":
                sortDirection = "asc";
                break;
            case "priority":
                sortField = "priority";
                break;
            case "status":
                sortField = "status";
                sortDirection = "asc";
                break;
        }

        Guid? parsedViewId = null;
        if (!string.IsNullOrEmpty(viewId))
        {
            if (ShortGuid.TryParse(viewId, out ShortGuid shortViewId))
            {
                parsedViewId = shortViewId.Guid;
            }
        }

        var snapshotPayload = new Application.Exports.Models.ExportSnapshotPayload
        {
            ViewId = parsedViewId,
            Filters = filters,
            SearchTerm = search,
            SortField = sortField,
            SortDirection = sortDirection,
            Columns = columns,
        };

        var command = new Application.Exports.Commands.CreateExportJob.Command
        {
            SnapshotPayload = snapshotPayload,
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            return RedirectToPage(RouteNames.Exports.Status, new { id = response.Result });
        }
        else
        {
            SetErrorMessage(
                !string.IsNullOrEmpty(response.Error) ? response.Error : "Failed to start export."
            );
            return RedirectToPage();
        }
    }

    /// <summary>
    /// Handles POST to unsnooze a ticket directly from the list.
    /// </summary>
    public async Task<IActionResult> OnPostUnsnoozeAsync(
        long ticketId,
        string? viewId = null,
        string? builtInView = null,
        string? search = null,
        string? sortBy = null,
        int? pageNumber = null,
        CancellationToken cancellationToken = default
    )
    {
        var response = await Mediator.Send(
            new Application.Tickets.Commands.UnsnoozeTicket.Command { TicketId = ticketId },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage($"Ticket #{ticketId} has been unsnoozed.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(
            RouteNames.Tickets.Index,
            new { viewId, builtInView, search, sortBy, pageNumber }
        );
    }

    private async Task<ViewConditions?> GetBuiltInViewConditionsAsync(
        string key,
        string? snoozeFilter,
        bool showSnoozed, // kept for backward compatibility but no longer used
        CancellationToken cancellationToken
    )
    {
        var currentUserId = CurrentUser.UserId?.Guid;

        // Calculate effective snooze filter based on view type and explicit filter
        // Defaults: "all" -> show all, "snoozed" -> only snoozed, others -> exclude snoozed
        var effectiveSnoozeFilter = !string.IsNullOrEmpty(snoozeFilter)
            ? snoozeFilter
            : key switch
            {
                "all" => "", // Show all by default
                "snoozed" => "snoozed", // Only snoozed by default
                "closed" or "recently-closed" => "", // No snooze filter for closed (they can't be snoozed)
                _ => "not-snoozed", // Exclude snoozed by default for all other views
            };

        // Helper for non-snoozed filter condition (use is_false operator)
        var notSnoozedCondition = new ViewFilterCondition
        {
            Field = "IsSnoozed",
            Operator = "is_false",
        };

        // Helper for snoozed-only filter condition (use is_true operator)
        var snoozedOnlyCondition = new ViewFilterCondition
        {
            Field = "IsSnoozed",
            Operator = "is_true",
        };

        // Build base conditions for each view (without snooze filters)
        List<ViewFilterCondition>? baseFilters = key switch
        {
            "all" => new List<ViewFilterCondition>(),
            "snoozed" => new List<ViewFilterCondition>(),
            "unassigned" => new List<ViewFilterCondition>
            {
                new() { Field = "AssigneeId", Operator = "isnull" },
                new() { Field = "OwningTeamId", Operator = "isnull" },
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "my-tickets" when currentUserId.HasValue => new List<ViewFilterCondition>
            {
                new() { Field = "AssigneeId", Operator = "equals", Value = new ShortGuid(currentUserId.Value).ToString() },
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "my-opened" or "created-by-me" when currentUserId.HasValue => new List<ViewFilterCondition>
            {
                new() { Field = "CreatedByStaffId", Operator = "equals", Value = new ShortGuid(currentUserId.Value).ToString() },
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "team-tickets" => new List<ViewFilterCondition>
            {
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "following" => new List<ViewFilterCondition>
            {
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "overdue" => new List<ViewFilterCondition>
            {
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
                new() { Field = "SlaStatus", Operator = "equals", Value = SlaStatus.BREACHED },
            },
            "open" => new List<ViewFilterCondition>
            {
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.OPEN },
            },
            "closed" or "recently-closed" => new List<ViewFilterCondition>
            {
                new() { Field = "StatusType", Operator = "equals", Value = TicketStatusType.CLOSED },
            },
            _ => null,
        };

        if (baseFilters == null)
            return null;

        // Apply snooze filter based on effective value
        if (effectiveSnoozeFilter == "snoozed")
        {
            baseFilters.Add(snoozedOnlyCondition);
        }
        else if (effectiveSnoozeFilter == "not-snoozed")
        {
            baseFilters.Add(notSnoozedCondition);
        }
        // else: empty string means show all (no snooze filter)

        // Return null if no filters (for "all" view with no snooze filter)
        if (baseFilters.Count == 0)
            return null;

        return new ViewConditions
        {
            Logic = "AND",
            Filters = baseFilters,
        };
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

        public string Language { get; init; } = string.Empty;

        [Display(Name = "Language")]
        public string LanguageLabel { get; init; } = string.Empty;

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

        // Task counts
        public int CompletedTaskCount { get; init; }
        public int TotalTaskCount { get; init; }

        // Snooze fields
        public bool IsSnoozed { get; init; }
        public DateTime? SnoozedUntil { get; init; }
        public string SnoozedUntilFormatted { get; init; } = string.Empty;
        public bool IsRecentlyUnsnoozed { get; init; }

        [Display(Name = "SLA Due")]
        public string SlaDueAt { get; init; } = string.Empty;

        [Display(Name = "SLA Status")]
        public string SlaStatusLabel { get; init; } = string.Empty;

        [Display(Name = "Created")]
        public string CreationTime { get; init; } = string.Empty;

        [Display(Name = "Last Updated")]
        public string LastModificationTime { get; init; } = string.Empty;

        [Display(Name = "Closed")]
        public string ClosedAt { get; init; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; init; } = string.Empty;

        [Display(Name = "Tags")]
        public string Tags { get; init; } = string.Empty;

        [Display(Name = "Created By")]
        public string CreatedByName { get; init; } = string.Empty;

        /// <summary>
        /// Whether the current user can edit this ticket.
        /// </summary>
        public bool CanEdit { get; init; }

        /// <summary>
        /// Gets column value by field name for dynamic rendering.
        /// </summary>
        public string GetColumnValue(string field) =>
            field switch
            {
                "Id" => $"#{Id}",
                "Title" => Title.Length > 50 ? Title.Substring(0, 47) + "..." : Title,
                "Status" => StatusLabel,
                "Priority" => PriorityLabel,
                "Language" => LanguageLabel,
                "Category" => Category,
                "AssigneeName" => !string.IsNullOrEmpty(OwningTeamName)
                    ? (
                        OwningTeamName
                        + (
                            !string.IsNullOrEmpty(AssigneeName) && AssigneeName != "Unassigned"
                                ? " / " + AssigneeName
                                : " / Anyone"
                        )
                    )
                    : (
                        string.IsNullOrEmpty(AssigneeName) || AssigneeName == "Unassigned"
                            ? "—"
                            : AssigneeName
                    ),
                "OwningTeamName" => string.IsNullOrEmpty(OwningTeamName) ? "—" : OwningTeamName,
                "ContactId" => ContactId.HasValue ? $"#{ContactId}" : "—",
                "ContactName" => string.IsNullOrEmpty(ContactName) || ContactName == "-"
                    ? "—"
                    : ContactName,
                "SlaStatus" => SlaStatusLabel == "-" ? "—" : SlaStatusLabel,
                "SlaDueAt" => SlaDueAt == "-" ? "—" : SlaDueAt,
                "CreationTime" => CreationTime,
                "LastModificationTime" => string.IsNullOrEmpty(LastModificationTime)
                    ? "—"
                    : LastModificationTime,
                "ClosedAt" => string.IsNullOrEmpty(ClosedAt) ? "—" : ClosedAt,
                "Description" => string.IsNullOrEmpty(Description)
                    ? "—"
                    : (
                        Description.Length > 50 ? Description.Substring(0, 47) + "..." : Description
                    ),
                "Tags" => string.IsNullOrEmpty(Tags) ? "—" : Tags,
                "CreatedByName" => string.IsNullOrEmpty(CreatedByName) ? "—" : CreatedByName,
                "IsSnoozed" => IsSnoozed ? "Yes" : "No",
                "SnoozedUntil" => string.IsNullOrEmpty(SnoozedUntilFormatted) ? "—" : SnoozedUntilFormatted,
                "Tasks" => TotalTaskCount > 0 ? $"{CompletedTaskCount} / {TotalTaskCount}" : "—",
                _ => "—",
            };
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
