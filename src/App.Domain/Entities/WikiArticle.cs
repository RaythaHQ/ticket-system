using App.Domain.Common;

namespace App.Domain.Entities;

/// <summary>
/// Represents a wiki article in the staff knowledge base.
/// </summary>
public class WikiArticle : BaseAuditableEntity
{
    /// <summary>
    /// The article title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly identifier for the article. Must be unique.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Rich text content of the article (HTML from TipTap editor).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional category for organizing articles.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Sort order within the category. Lower numbers appear first.
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Whether the article is published and visible to all staff.
    /// Unpublished articles are only visible to editors.
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Number of times the article has been viewed.
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Optional excerpt/summary for display in article lists.
    /// If not provided, a preview will be generated from content.
    /// </summary>
    public string? Excerpt { get; set; }

    /// <summary>
    /// Whether this article is pinned to the top of lists.
    /// </summary>
    public bool IsPinned { get; set; } = false;
}
