namespace App.Web.Areas.Shared.Models;

public interface IPaginationViewModel
{
    string Search { get; set; }
    string Filter { get; set; }
    int PageNumber { get; set; }
    int PageSize { get; set; }
    string OrderByPropertyName { get; set; }
    string OrderByDirection { get; set; }
    string OrderByAsString { get; }
    string PageName { get; set; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public string PreviousDisabledCss { get; }
    public string NextDisabledCss { get; }
    public int FirstVisiblePageNumber { get; }
    public int LastVisiblePageNumber { get; }

    /// <summary>
    /// Built-in view key (e.g., "unassigned", "my-tickets") for pagination retention.
    /// </summary>
    string? BuiltInView { get; set; }

    /// <summary>
    /// Custom view ID for pagination retention.
    /// </summary>
    string? ViewId { get; set; }
}

public abstract record PaginationViewModel : IPaginationViewModel
{
    public string Search { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 0;
    public int PageSize { get; set; } = 0;
    public string OrderByPropertyName { get; set; } = string.Empty;
    public string OrderByDirection { get; set; } = string.Empty;
    public string OrderByAsString =>
        string.IsNullOrEmpty(OrderByPropertyName) && string.IsNullOrEmpty(OrderByDirection)
            ? string.Empty
            : $"{OrderByPropertyName} {OrderByDirection}";
    public string PageName { get; set; }
    public int TotalCount { get; }
    public string? BuiltInView { get; set; }
    public string? ViewId { get; set; }

    public string PreviousDisabledCss =>
        TotalPages == 0 || PageNumber == 1 ? "disabled" : string.Empty;
    public string NextDisabledCss =>
        TotalPages == 0 || PageNumber == TotalPages ? "disabled" : string.Empty;
    public int FirstVisiblePageNumber => Math.Max(1, LastVisiblePageNumber - 3);
    public int LastVisiblePageNumber => Math.Min(TotalPages, Math.Max(1, PageNumber - 1) + 3);
    public int TotalPages => (int)Math.Ceiling((double)(TotalCount) / PageSize);

    public PaginationViewModel(int totalCount) => TotalCount = totalCount;
}

public record ListViewModel<T> : PaginationViewModel
{
    public IEnumerable<T> Items { get; }

    public ListViewModel(IEnumerable<T> items, int totalCount)
        : base(totalCount) => Items = items;
}
