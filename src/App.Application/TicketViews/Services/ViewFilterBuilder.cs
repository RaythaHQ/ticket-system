using App.Domain.Entities;
using App.Domain.ValueObjects;

namespace App.Application.TicketViews.Services;

/// <summary>
/// Builds IQueryable filters from view conditions.
/// </summary>
public class ViewFilterBuilder
{
    /// <summary>
    /// Apply view filter conditions to a ticket query.
    /// </summary>
    public IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, ViewConditions? conditions)
    {
        if (conditions == null || !conditions.Filters.Any())
            return query;

        // Build combined filter
        foreach (var filter in conditions.Filters)
        {
            query = ApplyFilter(query, filter);
        }

        return query;
    }

    private IQueryable<Ticket> ApplyFilter(IQueryable<Ticket> query, ViewFilterCondition filter)
    {
        return filter.Field.ToLower() switch
        {
            "status" => ApplyStringValueFilter(query, filter, t => t.Status),
            "priority" => ApplyStringValueFilter(query, filter, t => t.Priority),
            "category" => ApplyStringFilter(query, filter, t => t.Category),
            "slastatus" => ApplyStringFilter(query, filter, t => t.SlaStatus),
            "owningteamid" => ApplyGuidFilter(query, filter, t => t.OwningTeamId),
            "assigneeid" => ApplyGuidFilter(query, filter, t => t.AssigneeId),
            "createdbystaffid" => ApplyGuidFilter(query, filter, t => t.CreatedByStaffId),
            "contactid" => ApplyLongFilter(query, filter, t => t.ContactId),
            "creationtime" => ApplyDateFilter(query, filter, t => t.CreationTime),
            "lastmodificationtime" => ApplyNullableDateFilter(query, filter, t => t.LastModificationTime),
            "resolvedat" => ApplyNullableDateFilter(query, filter, t => t.ResolvedAt),
            "closedat" => ApplyNullableDateFilter(query, filter, t => t.ClosedAt),
            _ => query
        };
    }

    private IQueryable<Ticket> ApplyStringValueFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, string>> fieldSelector)
    {
        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            return query.Where(t => fieldSelector.Compile()(t).ToLower() == value);
        }
        else if (filter.Operator == "in" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            return query.Where(t => values.Contains(fieldSelector.Compile()(t).ToLower()));
        }
        else if (filter.Operator == "notin" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            return query.Where(t => !values.Contains(fieldSelector.Compile()(t).ToLower()));
        }
        return query;
    }

    private IQueryable<Ticket> ApplyStringFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, string?>> fieldSelector)
    {
        var field = fieldSelector.Compile();
        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            return query.Where(t => field(t) != null && field(t)!.ToLower() == value);
        }
        else if (filter.Operator == "contains" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            return query.Where(t => field(t) != null && field(t)!.ToLower().Contains(value));
        }
        else if (filter.Operator == "in" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            return query.Where(t => field(t) != null && values.Contains(field(t)!.ToLower()));
        }
        else if (filter.Operator == "notin" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            return query.Where(t => field(t) == null || !values.Contains(field(t)!.ToLower()));
        }
        else if (filter.Operator == "isnull")
        {
            return query.Where(t => field(t) == null);
        }
        else if (filter.Operator == "isnotnull")
        {
            return query.Where(t => field(t) != null);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyGuidFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, Guid?>> fieldSelector)
    {
        var field = fieldSelector.Compile();
        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value) && Guid.TryParse(filter.Value, out var guid))
        {
            return query.Where(t => field(t) == guid);
        }
        else if (filter.Operator == "isnull")
        {
            return query.Where(t => field(t) == null);
        }
        else if (filter.Operator == "isnotnull")
        {
            return query.Where(t => field(t) != null);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyLongFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, long?>> fieldSelector)
    {
        var field = fieldSelector.Compile();
        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value) && long.TryParse(filter.Value, out var id))
        {
            return query.Where(t => field(t) == id);
        }
        else if (filter.Operator == "isnull")
        {
            return query.Where(t => field(t) == null);
        }
        else if (filter.Operator == "isnotnull")
        {
            return query.Where(t => field(t) != null);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyDateFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, DateTime>> fieldSelector)
    {
        var field = fieldSelector.Compile();
        if (!string.IsNullOrEmpty(filter.Value) && DateTime.TryParse(filter.Value, out var date))
        {
            return filter.Operator switch
            {
                "gt" => query.Where(t => field(t) > date),
                "gte" => query.Where(t => field(t) >= date),
                "lt" => query.Where(t => field(t) < date),
                "lte" => query.Where(t => field(t) <= date),
                "equals" => query.Where(t => field(t).Date == date.Date),
                _ => query
            };
        }
        return query;
    }

    private IQueryable<Ticket> ApplyNullableDateFilter(IQueryable<Ticket> query, ViewFilterCondition filter, System.Linq.Expressions.Expression<Func<Ticket, DateTime?>> fieldSelector)
    {
        var field = fieldSelector.Compile();
        if (filter.Operator == "isnull")
        {
            return query.Where(t => field(t) == null);
        }
        else if (filter.Operator == "isnotnull")
        {
            return query.Where(t => field(t) != null);
        }
        else if (!string.IsNullOrEmpty(filter.Value) && DateTime.TryParse(filter.Value, out var date))
        {
            return filter.Operator switch
            {
                "gt" => query.Where(t => field(t) > date),
                "gte" => query.Where(t => field(t) >= date),
                "lt" => query.Where(t => field(t) < date),
                "lte" => query.Where(t => field(t) <= date),
                "equals" => query.Where(t => field(t).HasValue && field(t)!.Value.Date == date.Date),
                _ => query
            };
        }
        return query;
    }

    /// <summary>
    /// Apply column-limited search to query. Search only searches fields that are visible in the view.
    /// </summary>
    public IQueryable<Ticket> ApplyColumnSearch(IQueryable<Ticket> query, string? searchTerm, List<string> visibleColumns)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !visibleColumns.Any())
            return query;

        var term = searchTerm.Trim().ToLower();
        var normalizedColumns = visibleColumns.Select(c => c.ToLower()).ToList();

        // Build search based on visible columns only
        return query.Where(t =>
            (normalizedColumns.Contains("title") && t.Title.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("description") && t.Description != null && t.Description.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("category") && t.Category != null && t.Category.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("status") && t.Status.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("priority") && t.Priority.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("id") && t.Id.ToString().Contains(term)) ||
            (normalizedColumns.Contains("contactname") && t.Contact != null && t.Contact.Name.ToLower().Contains(term)) ||
            (normalizedColumns.Contains("assigneename") && t.Assignee != null && (t.Assignee.FirstName + " " + t.Assignee.LastName).ToLower().Contains(term)) ||
            (normalizedColumns.Contains("teamname") && t.OwningTeam != null && t.OwningTeam.Name.ToLower().Contains(term))
        );
    }
}
