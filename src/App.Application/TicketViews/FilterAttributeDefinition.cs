namespace App.Application.TicketViews;

/// <summary>
/// Defines a filterable/sortable attribute with its type and available operators.
/// </summary>
public record FilterAttributeDefinition
{
    public string Field { get; init; } = null!;
    public string Label { get; init; } = null!;

    /// <summary>
    /// Attribute type: string, date, boolean, numeric, selection, priority, user, team.
    /// </summary>
    public string Type { get; init; } = null!;

    public List<FilterOperatorDto> Operators { get; init; } = new();
    public bool IsSortable { get; init; } = true;
    public bool IsColumnVisible { get; init; } = true;
}

/// <summary>
/// Registry of all filterable and sortable attributes for ticket views.
/// </summary>
public static class FilterAttributes
{
    public static readonly IReadOnlyList<FilterAttributeDefinition> All = new List<FilterAttributeDefinition>
    {
        // Ticket Core
        new() { Field = "Id", Label = "Ticket ID", Type = "numeric", Operators = OperatorDefinitions.NumericOperators },
        new() { Field = "Title", Label = "Title", Type = "string", Operators = OperatorDefinitions.StringOperators },
        new() { Field = "Description", Label = "Description", Type = "string", Operators = OperatorDefinitions.StringOperators, IsColumnVisible = false },
        new() { Field = "Category", Label = "Category", Type = "string", Operators = OperatorDefinitions.StringOperators },
        new() { Field = "Tags", Label = "Tags", Type = "string", Operators = OperatorDefinitions.StringOperators },

        // Status & Priority
        new() { Field = "Status", Label = "Status", Type = "status", Operators = OperatorDefinitions.SelectionOperators },
        new() { Field = "Priority", Label = "Priority", Type = "priority", Operators = OperatorDefinitions.PriorityOperators },

        // Relationships
        new() { Field = "AssigneeId", Label = "Assignee", Type = "user", Operators = OperatorDefinitions.UserOperators },
        new() { Field = "CreatedByStaffId", Label = "Created By", Type = "user", Operators = OperatorDefinitions.UserOperators },
        new() { Field = "OwningTeamId", Label = "Team", Type = "team", Operators = OperatorDefinitions.SelectionOperators },
        new() { Field = "ContactId", Label = "Contact ID", Type = "numeric", Operators = OperatorDefinitions.NumericOperators },

        // Dates
        new() { Field = "CreationTime", Label = "Created At", Type = "date", Operators = OperatorDefinitions.DateOperators },
        new() { Field = "LastModificationTime", Label = "Updated At", Type = "date", Operators = OperatorDefinitions.DateOperators },
        new() { Field = "ClosedAt", Label = "Closed At", Type = "date", Operators = OperatorDefinitions.DateOperators },
        new() { Field = "SlaDueAt", Label = "SLA Due At", Type = "date", Operators = OperatorDefinitions.DateOperators },

        // Booleans
        new() { Field = "SlaBreached", Label = "SLA Breached", Type = "boolean", Operators = OperatorDefinitions.BooleanOperators },
        new() { Field = "HasContact", Label = "Has Contact", Type = "boolean", Operators = OperatorDefinitions.BooleanOperators },
        new() { Field = "HasAttachments", Label = "Has Attachments", Type = "boolean", Operators = OperatorDefinitions.BooleanOperators },

        // Contact Fields
        new() { Field = "Contact.FirstName", Label = "Contact First Name", Type = "string", Operators = OperatorDefinitions.StringOperators, IsSortable = false },
        new() { Field = "Contact.LastName", Label = "Contact Last Name", Type = "string", Operators = OperatorDefinitions.StringOperators, IsSortable = false },
        new() { Field = "Contact.Email", Label = "Contact Email", Type = "string", Operators = OperatorDefinitions.StringOperators, IsSortable = false },
        new() { Field = "Contact.Phone", Label = "Contact Phone", Type = "string", Operators = OperatorDefinitions.StringOperators, IsSortable = false },
        new() { Field = "Contact.OrganizationAccount", Label = "Contact Organization", Type = "string", Operators = OperatorDefinitions.StringOperators, IsSortable = false },
    };

    /// <summary>
    /// Get attribute definition by field name.
    /// </summary>
    public static FilterAttributeDefinition? GetByField(string field)
    {
        return All.FirstOrDefault(a => a.Field.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all sortable attributes.
    /// </summary>
    public static IEnumerable<FilterAttributeDefinition> GetSortable()
    {
        return All.Where(a => a.IsSortable);
    }
}

