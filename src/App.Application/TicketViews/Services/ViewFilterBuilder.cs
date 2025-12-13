using System.Linq.Expressions;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using static App.Domain.ValueObjects.FilterOperator;
using static App.Domain.ValueObjects.RelativeDatePreset;

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
/// Supports AND/OR logic, type-appropriate operators, and relative date filters.
/// </summary>
public class ViewFilterBuilder
{
    private TimeZoneInfo _timezone = TimeZoneInfo.Utc;

    /// <summary>
    /// Set the timezone for relative date calculations.
    /// </summary>
    public void SetTimezone(TimeZoneInfo timezone)
    {
        _timezone = timezone;
    }

    /// <summary>
    /// Apply view filter conditions to a ticket query.
    /// Logic: (all AndFilters must match) AND (if OrFilters exist, at least one must match).
    /// Falls back to legacy Filters field if AndFilters/OrFilters are empty.
    /// </summary>
    public IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, ViewConditions? conditions)
    {
        if (conditions == null)
            return query;

        // Use new AndFilters/OrFilters if present, otherwise fall back to legacy Filters
        var andFilters =
            conditions.AndFilters?.Any() == true
                ? conditions.AndFilters
                : (conditions.Logic != "OR" ? conditions.Filters : new List<ViewFilterCondition>());
        var orFilters =
            conditions.OrFilters?.Any() == true
                ? conditions.OrFilters
                : (conditions.Logic == "OR" ? conditions.Filters : new List<ViewFilterCondition>());

        // Step 1: Apply all AND filters (each must match)
        foreach (var filter in andFilters)
        {
            query = ApplyFilter(query, filter);
        }

        // Step 2: Apply OR filters (at least one must match) - only if there are any
        if (orFilters.Any())
        {
            query = ApplyOrFilters(query, orFilters);
        }

        return query;
    }

    /// <summary>
    /// Apply OR filters: at least one must match.
    /// </summary>
    private IQueryable<Ticket> ApplyOrFilters(
        IQueryable<Ticket> query,
        List<ViewFilterCondition> filters
    )
    {
        Expression<Func<Ticket, bool>>? combined = null;

        foreach (var filter in filters)
        {
            var filterExpr = BuildFilterExpression(filter);
            if (filterExpr == null)
                continue;

            if (combined == null)
            {
                combined = filterExpr;
            }
            else
            {
                var replacer = new ParameterReplacer(
                    filterExpr.Parameters[0],
                    combined.Parameters[0]
                );
                var newBody = replacer.Visit(filterExpr.Body);
                var orExpr = Expression.OrElse(combined.Body, newBody);
                combined = Expression.Lambda<Func<Ticket, bool>>(orExpr, combined.Parameters[0]);
            }
        }

        return combined != null ? query.Where(combined) : query;
    }

    /// <summary>
    /// Build a filter expression for a single condition (for OR logic).
    /// </summary>
    private Expression<Func<Ticket, bool>>? BuildFilterExpression(ViewFilterCondition filter)
    {
        var param = Expression.Parameter(typeof(Ticket), "t");
        var body = BuildFilterBody(filter, param);
        if (body == null)
            return null;
        return Expression.Lambda<Func<Ticket, bool>>(body, param);
    }

    /// <summary>
    /// Build the body of a filter expression.
    /// </summary>
    private Expression? BuildFilterBody(ViewFilterCondition filter, ParameterExpression param)
    {
        var fieldLower = filter.Field.ToLower();

        return fieldLower switch
        {
            // String fields
            "title" => BuildStringExpression(param, t => t.Title, filter),
            "description" => BuildNullableStringExpression(param, t => t.Description, filter),
            "category" => BuildNullableStringExpression(param, t => t.Category, filter),
            "tags" => BuildNullableStringExpression(param, t => t.TagsJson, filter),

            // Status with meta-groups
            "status" => BuildStatusExpression(param, filter),

            // Priority with comparison
            "priority" => BuildPriorityExpression(param, filter),

            // SLA Status
            "slastatus" => BuildNullableStringExpression(param, t => t.SlaStatus, filter),

            // Guid fields (user selection)
            "owningteamid" => BuildGuidExpression(param, t => t.OwningTeamId, filter),
            "assigneeid" => BuildGuidExpression(param, t => t.AssigneeId, filter),
            "createdbystaffid" => BuildGuidExpression(param, t => t.CreatedByStaffId, filter),

            // Numeric fields
            "id" => BuildLongExpression(param, t => t.Id, filter),
            "contactid" => BuildNullableLongExpression(param, t => t.ContactId, filter),

            // Date fields
            "creationtime" => BuildDateExpression(param, t => t.CreationTime, filter),
            "lastmodificationtime" => BuildNullableDateExpression(
                param,
                t => t.LastModificationTime,
                filter
            ),
            "resolvedat" => BuildNullableDateExpression(param, t => t.ResolvedAt, filter),
            "closedat" => BuildNullableDateExpression(param, t => t.ClosedAt, filter),
            "sladueat" => BuildNullableDateExpression(param, t => t.SlaDueAt, filter),

            // Boolean fields
            "slabreached" => BuildBooleanExpression(param, filter, t => t.SlaBreachedAt != null),
            "hascontact" => BuildBooleanExpression(param, filter, t => t.ContactId != null),
            "hasattachments" => BuildBooleanExpression(param, filter, t => t.Attachments.Any()),

            // Contact fields
            "contact.firstname" => BuildContactStringExpression(param, c => c.FirstName, filter),
            "contact.lastname" => BuildContactStringExpression(param, c => c.LastName, filter),
            "contact.emailaddress" or "contact.email" => BuildContactStringExpression(
                param,
                c => c.Email,
                filter
            ),
            "contact.phonenumber" or "contact.phone" => BuildContactStringExpression(
                param,
                c => c.PhoneNumbersJson,
                filter
            ),
            "contact.organization" or "contact.organizationaccount" => BuildContactStringExpression(
                param,
                c => c.OrganizationAccount,
                filter
            ),

            _ => null,
        };
    }

    private IQueryable<Ticket> ApplyFilter(IQueryable<Ticket> query, ViewFilterCondition filter)
    {
        var expr = BuildFilterExpression(filter);
        return expr != null ? query.Where(expr) : query;
    }

    #region String Operators

    private Expression? BuildStringExpression(
        ParameterExpression param,
        Expression<Func<Ticket, string>> selector,
        ViewFilterCondition filter
    )
    {
        var body = Expression.Invoke(selector, param);
        return BuildStringOperatorExpression(body, filter, false);
    }

    private Expression? BuildNullableStringExpression(
        ParameterExpression param,
        Expression<Func<Ticket, string?>> selector,
        ViewFilterCondition filter
    )
    {
        var body = Expression.Invoke(selector, param);
        return BuildStringOperatorExpression(body, filter, true);
    }

    private Expression? BuildStringOperatorExpression(
        Expression field,
        ViewFilterCondition filter,
        bool isNullable
    )
    {
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!;
        var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!;

        Expression notNullCheck = isNullable
            ? Expression.NotEqual(field, Expression.Constant(null, typeof(string)))
            : Expression.Constant(true);

        var value = filter.Value?.ToLower() ?? "";
        var valueConst = Expression.Constant(value);

        Expression fieldLower = isNullable
            ? Expression.Condition(
                Expression.NotEqual(field, Expression.Constant(null, typeof(string))),
                Expression.Call(field, toLowerMethod),
                Expression.Constant("")
            )
            : Expression.Call(field, toLowerMethod);

        return filter.Operator switch
        {
            EQ or EQUALS => Expression.AndAlso(
                notNullCheck,
                Expression.Equal(fieldLower, valueConst)
            ),
            NEQ => Expression.OrElse(
                Expression.Equal(field, Expression.Constant(null, typeof(string))),
                Expression.NotEqual(fieldLower, valueConst)
            ),
            CONTAINS => Expression.AndAlso(
                notNullCheck,
                Expression.Call(fieldLower, containsMethod, valueConst)
            ),
            NOT_CONTAINS => Expression.OrElse(
                Expression.Equal(field, Expression.Constant(null, typeof(string))),
                Expression.Not(Expression.Call(fieldLower, containsMethod, valueConst))
            ),
            STARTS_WITH => Expression.AndAlso(
                notNullCheck,
                Expression.Call(fieldLower, startsWithMethod, valueConst)
            ),
            NOT_STARTS_WITH => Expression.OrElse(
                Expression.Equal(field, Expression.Constant(null, typeof(string))),
                Expression.Not(Expression.Call(fieldLower, startsWithMethod, valueConst))
            ),
            ENDS_WITH => Expression.AndAlso(
                notNullCheck,
                Expression.Call(fieldLower, endsWithMethod, valueConst)
            ),
            NOT_ENDS_WITH => Expression.OrElse(
                Expression.Equal(field, Expression.Constant(null, typeof(string))),
                Expression.Not(Expression.Call(fieldLower, endsWithMethod, valueConst))
            ),
            IS_EMPTY => isNullable
                ? Expression.OrElse(
                    Expression.Equal(field, Expression.Constant(null, typeof(string))),
                    Expression.Equal(field, Expression.Constant(""))
                )
                : Expression.Equal(field, Expression.Constant("")),
            IS_NOT_EMPTY => Expression.AndAlso(
                Expression.NotEqual(field, Expression.Constant(null, typeof(string))),
                Expression.NotEqual(field, Expression.Constant(""))
            ),
            _ => null,
        };
    }

    #endregion

    #region Status Operators (with meta-groups)

    private Expression? BuildStatusExpression(ParameterExpression param, ViewFilterCondition filter)
    {
        var statusField = Expression.Property(param, nameof(Ticket.Status));
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var statusLower = Expression.Call(statusField, toLowerMethod);

        // Handle meta-groups
        if (filter.Value == "__OPEN__" || filter.Value?.ToLower() == "open")
        {
            // Open = not closed and not resolved
            var closedConst = Expression.Constant(TicketStatus.CLOSED);
            var resolvedConst = Expression.Constant(TicketStatus.RESOLVED);
            return Expression.AndAlso(
                Expression.NotEqual(statusLower, closedConst),
                Expression.NotEqual(statusLower, resolvedConst)
            );
        }

        if (filter.Value == "__CLOSED__" || filter.Value?.ToLower() == "closed_meta")
        {
            // Closed = closed or resolved
            var closedConst = Expression.Constant(TicketStatus.CLOSED);
            var resolvedConst = Expression.Constant(TicketStatus.RESOLVED);
            return Expression.OrElse(
                Expression.Equal(statusLower, closedConst),
                Expression.Equal(statusLower, resolvedConst)
            );
        }

        // Standard operators
        var value = filter.Value?.ToLower() ?? "";
        var valueConst = Expression.Constant(value);

        return filter.Operator switch
        {
            IS or EQ or EQUALS => Expression.Equal(statusLower, valueConst),
            IS_NOT or NEQ => Expression.NotEqual(statusLower, valueConst),
            IS_ANY_OF or IN when filter.Values?.Any() == true => BuildContainsExpression(
                statusLower,
                filter.Values
            ),
            IS_NONE_OF or NOT_IN when filter.Values?.Any() == true => Expression.Not(
                BuildContainsExpression(statusLower, filter.Values)
            ),
            _ => null,
        };
    }

    #endregion

    #region Priority Operators (with comparison)

    private Expression? BuildPriorityExpression(
        ParameterExpression param,
        ViewFilterCondition filter
    )
    {
        var priorityField = Expression.Property(param, nameof(Ticket.Priority));
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var priorityLower = Expression.Call(priorityField, toLowerMethod);

        var value = filter.Value?.ToLower() ?? "";
        var valueConst = Expression.Constant(value);

        // For comparison operators, we need to use SortOrder
        if (filter.Operator is GT or LT or GTE or LTE)
        {
            var threshold = TicketPriority.From(filter.Value ?? "normal").SortOrder;

            // Build expression to get priority sort order
            // This requires a case expression or lookup
            var sortOrderExpr = BuildPrioritySortOrderExpression(priorityLower);

            return filter.Operator switch
            {
                GT => Expression.GreaterThan(sortOrderExpr, Expression.Constant(threshold)),
                LT => Expression.LessThan(sortOrderExpr, Expression.Constant(threshold)),
                GTE => Expression.GreaterThanOrEqual(sortOrderExpr, Expression.Constant(threshold)),
                LTE => Expression.LessThanOrEqual(sortOrderExpr, Expression.Constant(threshold)),
                _ => null,
            };
        }

        return filter.Operator switch
        {
            IS or EQ or EQUALS => Expression.Equal(priorityLower, valueConst),
            IS_NOT or NEQ => Expression.NotEqual(priorityLower, valueConst),
            IS_ANY_OF or IN when filter.Values?.Any() == true => BuildContainsExpression(
                priorityLower,
                filter.Values
            ),
            IS_NONE_OF or NOT_IN when filter.Values?.Any() == true => Expression.Not(
                BuildContainsExpression(priorityLower, filter.Values)
            ),
            _ => null,
        };
    }

    private Expression BuildPrioritySortOrderExpression(Expression priorityLower)
    {
        // Build: priority == "urgent" ? 4 : priority == "high" ? 3 : priority == "normal" ? 2 : 1
        return Expression.Condition(
            Expression.Equal(priorityLower, Expression.Constant(TicketPriority.URGENT)),
            Expression.Constant(4),
            Expression.Condition(
                Expression.Equal(priorityLower, Expression.Constant(TicketPriority.HIGH)),
                Expression.Constant(3),
                Expression.Condition(
                    Expression.Equal(priorityLower, Expression.Constant(TicketPriority.NORMAL)),
                    Expression.Constant(2),
                    Expression.Constant(1)
                )
            )
        );
    }

    #endregion

    #region Guid/User Operators

    private Expression? BuildGuidExpression(
        ParameterExpression param,
        Expression<Func<Ticket, Guid?>> selector,
        ViewFilterCondition filter
    )
    {
        var field = Expression.Invoke(selector, param);

        if (filter.Operator is IS_EMPTY or IS_NULL)
        {
            return Expression.Equal(field, Expression.Constant(null, typeof(Guid?)));
        }

        if (filter.Operator is IS_NOT_EMPTY or IS_NOT_NULL)
        {
            return Expression.NotEqual(field, Expression.Constant(null, typeof(Guid?)));
        }

        // Parse value as Guid
        Guid? guid = ParseGuid(filter.Value);

        if (filter.Operator is IS or EQ or EQUALS && guid.HasValue)
        {
            return Expression.Equal(field, Expression.Constant(guid, typeof(Guid?)));
        }

        if (filter.Operator is IS_NOT or NEQ && guid.HasValue)
        {
            return Expression.NotEqual(field, Expression.Constant(guid, typeof(Guid?)));
        }

        if (filter.Operator is IS_ANY_OF && filter.Values?.Any() == true)
        {
            var guids = filter
                .Values.Select(ParseGuid)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
            if (!guids.Any())
                return null;
            return BuildGuidContainsExpression(field, guids);
        }

        if (filter.Operator is IS_NONE_OF && filter.Values?.Any() == true)
        {
            var guids = filter
                .Values.Select(ParseGuid)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
            if (!guids.Any())
                return null;
            return Expression.Not(BuildGuidContainsExpression(field, guids));
        }

        return null;
    }

    private Guid? ParseGuid(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;
        // Try standard Guid first to avoid ShortGuid ambiguity
        if (Guid.TryParse(value, out var guid))
            return guid;
        // Try ShortGuid parsing
        if (ShortGuid.TryParse(value, out ShortGuid shortGuid))
        {
            return shortGuid.Guid;
        }
        return null;
    }

    private Expression BuildGuidContainsExpression(Expression field, List<Guid> guids)
    {
        var containsMethod = typeof(List<Guid?>).GetMethod("Contains", new[] { typeof(Guid?) })!;
        var guidList = guids.Select(g => (Guid?)g).ToList();
        return Expression.Call(Expression.Constant(guidList), containsMethod, field);
    }

    #endregion

    #region Numeric Operators

    private Expression? BuildLongExpression(
        ParameterExpression param,
        Expression<Func<Ticket, long>> selector,
        ViewFilterCondition filter
    )
    {
        var field = Expression.Invoke(selector, param);
        if (!long.TryParse(filter.Value, out var value))
            return null;
        var valueConst = Expression.Constant(value);

        return filter.Operator switch
        {
            "eq" or "equals" => Expression.Equal(field, valueConst),
            "neq" => Expression.NotEqual(field, valueConst),
            GT => Expression.GreaterThan(field, valueConst),
            LT => Expression.LessThan(field, valueConst),
            GTE => Expression.GreaterThanOrEqual(field, valueConst),
            LTE => Expression.LessThanOrEqual(field, valueConst),
            _ => null,
        };
    }

    private Expression? BuildNullableLongExpression(
        ParameterExpression param,
        Expression<Func<Ticket, long?>> selector,
        ViewFilterCondition filter
    )
    {
        var field = Expression.Invoke(selector, param);

        if (filter.Operator is IS_EMPTY or IS_NULL)
        {
            return Expression.Equal(field, Expression.Constant(null, typeof(long?)));
        }

        if (filter.Operator is IS_NOT_EMPTY or IS_NOT_NULL)
        {
            return Expression.NotEqual(field, Expression.Constant(null, typeof(long?)));
        }

        if (!long.TryParse(filter.Value, out var value))
            return null;
        var valueConst = Expression.Constant((long?)value, typeof(long?));

        return filter.Operator switch
        {
            "eq" or "equals" => Expression.Equal(field, valueConst),
            "neq" => Expression.NotEqual(field, valueConst),
            GT => Expression.GreaterThan(field, valueConst),
            LT => Expression.LessThan(field, valueConst),
            GTE => Expression.GreaterThanOrEqual(field, valueConst),
            LTE => Expression.LessThanOrEqual(field, valueConst),
            _ => null,
        };
    }

    #endregion

    #region Date Operators

    private Expression? BuildDateExpression(
        ParameterExpression param,
        Expression<Func<Ticket, DateTime>> selector,
        ViewFilterCondition filter
    )
    {
        var field = Expression.Invoke(selector, param);
        
        // Handle "is within" operators with number value
        if (filter.Operator.StartsWith("is_within_"))
        {
            var (withinStart, withinEnd) = ResolveWithinDateRange(filter);
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(field, Expression.Constant(withinStart)),
                Expression.LessThanOrEqual(field, Expression.Constant(withinEnd))
            );
        }
        
        var (startDate, endDate) = ResolveDateValue(filter);

        return filter.Operator switch
        {
            "is" => Expression.AndAlso(
                Expression.GreaterThanOrEqual(field, Expression.Constant(startDate)),
                Expression.LessThanOrEqual(field, Expression.Constant(endDate))
            ),
            IS_BEFORE => Expression.LessThan(field, Expression.Constant(startDate)),
            IS_AFTER => Expression.GreaterThan(field, Expression.Constant(endDate)),
            IS_ON_OR_BEFORE => Expression.LessThanOrEqual(field, Expression.Constant(endDate)),
            IS_ON_OR_AFTER => Expression.GreaterThanOrEqual(field, Expression.Constant(startDate)),
            // Legacy operators
            GT => Expression.GreaterThan(field, Expression.Constant(startDate)),
            GTE => Expression.GreaterThanOrEqual(field, Expression.Constant(startDate)),
            LT => Expression.LessThan(field, Expression.Constant(startDate)),
            LTE => Expression.LessThanOrEqual(field, Expression.Constant(startDate)),
            "equals" => Expression.AndAlso(
                Expression.GreaterThanOrEqual(field, Expression.Constant(startDate)),
                Expression.LessThanOrEqual(field, Expression.Constant(endDate))
            ),
            _ => null,
        };
    }

    private Expression? BuildNullableDateExpression(
        ParameterExpression param,
        Expression<Func<Ticket, DateTime?>> selector,
        ViewFilterCondition filter
    )
    {
        var field = Expression.Invoke(selector, param);

        if (filter.Operator is IS_EMPTY or IS_NULL)
        {
            return Expression.Equal(field, Expression.Constant(null, typeof(DateTime?)));
        }

        if (filter.Operator is IS_NOT_EMPTY or IS_NOT_NULL)
        {
            return Expression.NotEqual(field, Expression.Constant(null, typeof(DateTime?)));
        }

        // Handle "is within" operators with number value
        if (filter.Operator.StartsWith("is_within_"))
        {
            var (withinStart, withinEnd) = ResolveWithinDateRange(filter);
            return Expression.AndAlso(
                Expression.GreaterThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)withinStart, typeof(DateTime?))
                ),
                Expression.LessThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)withinEnd, typeof(DateTime?))
                )
            );
        }

        var (startDate, endDate) = ResolveDateValue(filter);

        return filter.Operator switch
        {
            IS => Expression.AndAlso(
                Expression.GreaterThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)startDate, typeof(DateTime?))
                ),
                Expression.LessThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)endDate, typeof(DateTime?))
                )
            ),
            IS_BEFORE => Expression.LessThan(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            IS_AFTER => Expression.GreaterThan(
                field,
                Expression.Constant((DateTime?)endDate, typeof(DateTime?))
            ),
            IS_ON_OR_BEFORE => Expression.LessThanOrEqual(
                field,
                Expression.Constant((DateTime?)endDate, typeof(DateTime?))
            ),
            IS_ON_OR_AFTER => Expression.GreaterThanOrEqual(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            // Legacy operators
            GT => Expression.GreaterThan(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            GTE => Expression.GreaterThanOrEqual(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            LT => Expression.LessThan(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            LTE => Expression.LessThanOrEqual(
                field,
                Expression.Constant((DateTime?)startDate, typeof(DateTime?))
            ),
            "equals" => Expression.AndAlso(
                Expression.GreaterThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)startDate, typeof(DateTime?))
                ),
                Expression.LessThanOrEqual(
                    field,
                    Expression.Constant((DateTime?)endDate, typeof(DateTime?))
                )
            ),
            _ => null,
        };
    }

    private (DateTime Start, DateTime End) ResolveDateValue(ViewFilterCondition filter)
    {
        // Handle relative date presets
        if (!string.IsNullOrEmpty(filter.RelativeDatePreset))
        {
            return RelativeDatePresets.Resolve(
                filter.RelativeDatePreset,
                filter.RelativeDateValue,
                _timezone
            );
        }

        // Handle exact date value
        if (DateTime.TryParse(filter.Value, out var date))
        {
            return (date.Date, date.Date.AddDays(1).AddTicks(-1));
        }

        // Default to today
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timezone);
        return (now.Date, now.Date.AddDays(1).AddTicks(-1));
    }

    /// <summary>
    /// Resolve date range for "is_within_past/next_hours/days/months" operators.
    /// The Value field contains the number (e.g., "7" for 7 days).
    /// </summary>
    private (DateTime Start, DateTime End) ResolveWithinDateRange(ViewFilterCondition filter)
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timezone);
        var amount = int.TryParse(filter.Value, out var val) ? val : 1;

        return filter.Operator switch
        {
            IS_WITHIN_PAST_HOURS => (now.AddHours(-amount), now),
            IS_WITHIN_PAST_DAYS => (now.AddDays(-amount), now),
            IS_WITHIN_PAST_MONTHS => (now.AddMonths(-amount), now),
            IS_WITHIN_NEXT_HOURS => (now, now.AddHours(amount)),
            IS_WITHIN_NEXT_DAYS => (now, now.AddDays(amount)),
            IS_WITHIN_NEXT_MONTHS => (now, now.AddMonths(amount)),
            _ => (now.AddDays(-1), now) // fallback
        };
    }

    #endregion

    #region Boolean Operators

    private Expression? BuildBooleanExpression(
        ParameterExpression param,
        ViewFilterCondition filter,
        Expression<Func<Ticket, bool>> selector
    )
    {
        var field = Expression.Invoke(selector, param);

        return filter.Operator switch
        {
            IS_TRUE => field,
            IS_FALSE => Expression.Not(field),
            _ => null,
        };
    }

    #endregion

    #region Contact Field Operators

    private Expression? BuildContactStringExpression(
        ParameterExpression param,
        Expression<Func<Contact, string?>> contactSelector,
        ViewFilterCondition filter
    )
    {
        // t.Contact != null && [string filter on contact field]
        var contactField = Expression.Property(param, nameof(Ticket.Contact));
        var contactNotNull = Expression.NotEqual(
            contactField,
            Expression.Constant(null, typeof(Contact))
        );

        // Build the contact field access
        var contactParam = Expression.Parameter(typeof(Contact), "c");
        var contactFieldBody = Expression.Invoke(contactSelector, contactField);

        var stringExpr = BuildStringOperatorExpression(contactFieldBody, filter, true);
        if (stringExpr == null)
            return null;

        return Expression.AndAlso(contactNotNull, stringExpr);
    }

    #endregion

    #region Helper Methods

    private Expression BuildContainsExpression(Expression field, List<string> values)
    {
        var normalizedValues = values.Select(v => v.ToLower()).ToList();
        var containsMethod = typeof(List<string>).GetMethod("Contains", new[] { typeof(string) })!;
        return Expression.Call(Expression.Constant(normalizedValues), containsMethod, field);
    }

    #endregion

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
                && (
                    t.Contact.FirstName.ToLower().Contains(term)
                    || (t.Contact.LastName != null && t.Contact.LastName.ToLower().Contains(term))
                )
            )
            || (
                normalizedColumns.Contains("assigneename")
                && t.Assignee != null
                && (t.Assignee.FirstName + " " + t.Assignee.LastName).ToLower().Contains(term)
            )
            || (
                normalizedColumns.Contains("teamname")
                || normalizedColumns.Contains("owningteamname")
                    && t.OwningTeam != null
                    && t.OwningTeam.Name.ToLower().Contains(term)
            )
            || (
                normalizedColumns.Contains("createdbyname")
                && t.CreatedByStaff != null
                && (t.CreatedByStaff.FirstName + " " + t.CreatedByStaff.LastName)
                    .ToLower()
                    .Contains(term)
            )
            || (
                normalizedColumns.Contains("tags")
                && t.TagsJson != null
                && t.TagsJson.ToLower().Contains(term)
            )
            || (
                normalizedColumns.Contains("contactid")
                && t.ContactId != null
                && t.ContactId.ToString()!.Contains(term)
            )
        );
    }

    /// <summary>
    /// Apply multi-level sorting to a ticket query.
    /// </summary>
    public IQueryable<Ticket> ApplySorting(IQueryable<Ticket> query, List<ViewSortLevel> sortLevels)
    {
        if (!sortLevels.Any())
            return query.OrderByDescending(t => t.CreationTime);

        IOrderedQueryable<Ticket>? orderedQuery = null;

        foreach (var level in sortLevels.OrderBy(s => s.Order))
        {
            orderedQuery =
                orderedQuery == null
                    ? ApplyPrimarySort(query, level)
                    : ApplySecondarySort(orderedQuery, level);
        }

        return orderedQuery ?? query;
    }

    private IOrderedQueryable<Ticket> ApplyPrimarySort(
        IQueryable<Ticket> query,
        ViewSortLevel level
    )
    {
        var keySelector = GetSortKeySelector(level.Field);
        return level.Direction == "desc"
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    private IOrderedQueryable<Ticket> ApplySecondarySort(
        IOrderedQueryable<Ticket> query,
        ViewSortLevel level
    )
    {
        var keySelector = GetSortKeySelector(level.Field);
        return level.Direction == "desc"
            ? query.ThenByDescending(keySelector)
            : query.ThenBy(keySelector);
    }

    private Expression<Func<Ticket, object>> GetSortKeySelector(string field)
    {
        return field.ToLower() switch
        {
            "id" => t => t.Id,
            "title" => t => t.Title,
            "status" => t => t.Status,
            "priority" => t => t.Priority,
            "category" => t => t.Category ?? "",
            "creationtime" => t => t.CreationTime,
            "lastmodificationtime" => t => t.LastModificationTime ?? DateTime.MinValue,
            "closedat" => t => t.ClosedAt ?? DateTime.MinValue,
            "sladueat" => t => t.SlaDueAt ?? DateTime.MaxValue,
            "slastatus" => t => t.SlaStatus ?? "",
            "assigneename" => t =>
                t.Assignee != null ? t.Assignee.FirstName + " " + t.Assignee.LastName : "",
            "owningteamname" => t => t.OwningTeam != null ? t.OwningTeam.Name : "",
            "contactname" => t =>
                t.Contact != null ? t.Contact.FirstName + " " + (t.Contact.LastName ?? "") : "",
            "createdbyname" => t =>
                t.CreatedByStaff != null
                    ? t.CreatedByStaff.FirstName + " " + t.CreatedByStaff.LastName
                    : "",
            _ => t => t.CreationTime,
        };
    }
}
