using System.Linq.Expressions;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.TicketViews.Services;

/// <summary>
/// Expression visitor to replace parameter in expression trees.
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}

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
            "lastmodificationtime" => ApplyNullableDateFilter(
                query,
                filter,
                t => t.LastModificationTime
            ),
            "resolvedat" => ApplyNullableDateFilter(query, filter, t => t.ResolvedAt),
            "closedat" => ApplyNullableDateFilter(query, filter, t => t.ClosedAt),
            _ => query,
        };
    }

    private IQueryable<Ticket> ApplyStringValueFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, string>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var constant = Expression.Constant(value);
            var equals = Expression.Equal(toLower, constant);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(equals, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "in" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var containsMethod = typeof(List<string>).GetMethod(
                "Contains",
                new[] { typeof(string) }
            )!;
            var constant = Expression.Constant(values);
            var contains = Expression.Call(constant, containsMethod, toLower);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(contains, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "notin" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var containsMethod = typeof(List<string>).GetMethod(
                "Contains",
                new[] { typeof(string) }
            )!;
            var constant = Expression.Constant(values);
            var contains = Expression.Call(constant, containsMethod, toLower);
            var notContains = Expression.Not(contains);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(notContains, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyStringFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, string?>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var constant = Expression.Constant(value);
            var equals = Expression.Equal(toLower, constant);
            var and = Expression.AndAlso(notNull, equals);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(and, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "contains" && !string.IsNullOrEmpty(filter.Value))
        {
            var value = filter.Value.ToLower();
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var constant = Expression.Constant(value);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            var contains = Expression.Call(toLower, containsMethod, constant);
            var and = Expression.AndAlso(notNull, contains);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(and, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "in" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var containsMethod = typeof(List<string>).GetMethod(
                "Contains",
                new[] { typeof(string) }
            )!;
            var constant = Expression.Constant(values);
            var contains = Expression.Call(constant, containsMethod, toLower);
            var and = Expression.AndAlso(notNull, contains);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(and, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "notin" && filter.Values?.Any() == true)
        {
            var values = filter.Values.Select(v => v.ToLower()).ToList();
            var isNull = Expression.Equal(body, Expression.Constant(null, typeof(string)));
            var toLower = Expression.Call(
                body,
                typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
            );
            var containsMethod = typeof(List<string>).GetMethod(
                "Contains",
                new[] { typeof(string) }
            )!;
            var constant = Expression.Constant(values);
            var contains = Expression.Call(constant, containsMethod, toLower);
            var notContains = Expression.Not(contains);
            var or = Expression.OrElse(isNull, notContains);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(or, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnull")
        {
            var isNull = Expression.Equal(body, Expression.Constant(null, typeof(string)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(isNull, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnotnull")
        {
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(string)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(notNull, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyGuidFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, Guid?>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (filter.Operator == "equals" && !string.IsNullOrEmpty(filter.Value))
        {
            // Try parsing as ShortGuid first (preferred), then fall back to Guid
            Guid? guid = null;
            ShortGuid shortGuid;
            if (ShortGuid.TryParse(filter.Value, out shortGuid))
            {
                guid = shortGuid.Guid;
            }
            else if (Guid.TryParse(filter.Value, out var parsedGuid))
            {
                guid = parsedGuid;
            }

            if (guid.HasValue)
            {
                var constant = Expression.Constant(guid.Value, typeof(Guid?));
                var equals = Expression.Equal(body, constant);
                var lambda = Expression.Lambda<Func<Ticket, bool>>(equals, parameter);
                return query.Where(lambda);
            }
        }
        else if (filter.Operator == "isnull")
        {
            var isNull = Expression.Equal(body, Expression.Constant(null, typeof(Guid?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(isNull, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnotnull")
        {
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(Guid?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(notNull, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyLongFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, long?>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (
            filter.Operator == "equals"
            && !string.IsNullOrEmpty(filter.Value)
            && long.TryParse(filter.Value, out var id)
        )
        {
            var constant = Expression.Constant(id, typeof(long?));
            var equals = Expression.Equal(body, constant);
            var lambda = Expression.Lambda<Func<Ticket, bool>>(equals, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnull")
        {
            var isNull = Expression.Equal(body, Expression.Constant(null, typeof(long?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(isNull, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnotnull")
        {
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(long?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(notNull, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyDateFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, DateTime>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (!string.IsNullOrEmpty(filter.Value) && DateTime.TryParse(filter.Value, out var date))
        {
            Expression comparison;
            switch (filter.Operator)
            {
                case "gt":
                    comparison = Expression.GreaterThan(body, Expression.Constant(date));
                    break;
                case "gte":
                    comparison = Expression.GreaterThanOrEqual(body, Expression.Constant(date));
                    break;
                case "lt":
                    comparison = Expression.LessThan(body, Expression.Constant(date));
                    break;
                case "lte":
                    comparison = Expression.LessThanOrEqual(body, Expression.Constant(date));
                    break;
                case "equals":
                    var dateProperty = typeof(DateTime).GetProperty("Date")!;
                    var bodyDate = Expression.Property(body, dateProperty);
                    var dateDate = Expression.Constant(date.Date);
                    comparison = Expression.Equal(bodyDate, dateDate);
                    break;
                default:
                    return query;
            }
            var lambda = Expression.Lambda<Func<Ticket, bool>>(comparison, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    private IQueryable<Ticket> ApplyNullableDateFilter(
        IQueryable<Ticket> query,
        ViewFilterCondition filter,
        Expression<Func<Ticket, DateTime?>> fieldSelector
    )
    {
        var parameter = Expression.Parameter(typeof(Ticket), "t");
        var replacer = new ParameterReplacer(fieldSelector.Parameters[0], parameter);
        var body = replacer.Visit(fieldSelector.Body);

        if (filter.Operator == "isnull")
        {
            var isNull = Expression.Equal(body, Expression.Constant(null, typeof(DateTime?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(isNull, parameter);
            return query.Where(lambda);
        }
        else if (filter.Operator == "isnotnull")
        {
            var notNull = Expression.NotEqual(body, Expression.Constant(null, typeof(DateTime?)));
            var lambda = Expression.Lambda<Func<Ticket, bool>>(notNull, parameter);
            return query.Where(lambda);
        }
        else if (
            !string.IsNullOrEmpty(filter.Value) && DateTime.TryParse(filter.Value, out var date)
        )
        {
            Expression comparison;
            switch (filter.Operator)
            {
                case "gt":
                    comparison = Expression.GreaterThan(
                        body,
                        Expression.Constant(date, typeof(DateTime?))
                    );
                    break;
                case "gte":
                    comparison = Expression.GreaterThanOrEqual(
                        body,
                        Expression.Constant(date, typeof(DateTime?))
                    );
                    break;
                case "lt":
                    comparison = Expression.LessThan(
                        body,
                        Expression.Constant(date, typeof(DateTime?))
                    );
                    break;
                case "lte":
                    comparison = Expression.LessThanOrEqual(
                        body,
                        Expression.Constant(date, typeof(DateTime?))
                    );
                    break;
                case "equals":
                    var hasValueProperty = typeof(DateTime?).GetProperty("HasValue")!;
                    var valueProperty = typeof(DateTime?).GetProperty("Value")!;
                    var dateProperty = typeof(DateTime).GetProperty("Date")!;
                    var hasValue = Expression.Property(body, hasValueProperty);
                    var value = Expression.Property(body, valueProperty);
                    var valueDate = Expression.Property(value, dateProperty);
                    var dateDate = Expression.Constant(date.Date);
                    var dateEquals = Expression.Equal(valueDate, dateDate);
                    comparison = Expression.AndAlso(hasValue, dateEquals);
                    break;
                default:
                    return query;
            }
            var lambda = Expression.Lambda<Func<Ticket, bool>>(comparison, parameter);
            return query.Where(lambda);
        }
        return query;
    }

    /// <summary>
    /// Apply column-limited search to query. Search only searches fields that are visible in the view.
    /// </summary>
    public IQueryable<Ticket> ApplyColumnSearch(
        IQueryable<Ticket> query,
        string? searchTerm,
        List<string> visibleColumns
    )
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || !visibleColumns.Any())
            return query;

        var term = searchTerm.Trim().ToLower();
        var normalizedColumns = visibleColumns.Select(c => c.ToLower()).ToList();

        // Build search based on visible columns only
        return query.Where(t =>
            (normalizedColumns.Contains("title") && t.Title.ToLower().Contains(term))
            || (
                normalizedColumns.Contains("description")
                && t.Description != null
                && t.Description.ToLower().Contains(term)
            )
            || (
                normalizedColumns.Contains("category")
                && t.Category != null
                && t.Category.ToLower().Contains(term)
            )
            || (normalizedColumns.Contains("status") && t.Status.ToLower().Contains(term))
            || (normalizedColumns.Contains("priority") && t.Priority.ToLower().Contains(term))
            || (normalizedColumns.Contains("id") && t.Id.ToString().Contains(term))
            || (
                normalizedColumns.Contains("contactname")
                && t.Contact != null
                && (t.Contact.FirstName.ToLower().Contains(term)
                    || (t.Contact.LastName != null && t.Contact.LastName.ToLower().Contains(term)))
            )
            || (
                normalizedColumns.Contains("assigneename")
                && t.Assignee != null
                && (t.Assignee.FirstName + " " + t.Assignee.LastName).ToLower().Contains(term)
            )
            || (
                normalizedColumns.Contains("teamname")
                && t.OwningTeam != null
                && t.OwningTeam.Name.ToLower().Contains(term)
            )
        );
    }
}
