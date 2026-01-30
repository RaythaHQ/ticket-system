using System.Text.RegularExpressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Wiki;

/// <summary>
/// Wiki article data transfer object.
/// </summary>
public record WikiArticleDto : BaseAuditableEntityDto
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? Category { get; init; }
    public int SortOrder { get; init; }
    public bool IsPublished { get; init; }
    public int ViewCount { get; init; }
    public string? Excerpt { get; init; }
    public bool IsPinned { get; init; }

    /// <summary>
    /// Auto-generated excerpt from content if Excerpt is not set.
    /// </summary>
    public string DisplayExcerpt => !string.IsNullOrEmpty(Excerpt)
        ? Excerpt
        : GenerateExcerptFromContent(Content, 200);

    /// <summary>
    /// Creator's full name for display.
    /// </summary>
    public string? CreatorName { get; init; }

    /// <summary>
    /// Last modifier's full name for display.
    /// </summary>
    public string? LastModifierName { get; init; }

    public static WikiArticleDto MapFrom(WikiArticle article)
    {
        return new WikiArticleDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Content = article.Content,
            Category = article.Category,
            SortOrder = article.SortOrder,
            IsPublished = article.IsPublished,
            ViewCount = article.ViewCount,
            Excerpt = article.Excerpt,
            IsPinned = article.IsPinned,
            CreationTime = article.CreationTime,
            LastModificationTime = article.LastModificationTime,
            CreatorName = article.CreatorUser != null
                ? $"{article.CreatorUser.FirstName} {article.CreatorUser.LastName}".Trim()
                : null,
            LastModifierName = article.LastModifierUser != null
                ? $"{article.LastModifierUser.FirstName} {article.LastModifierUser.LastName}".Trim()
                : null,
        };
    }

    private static string GenerateExcerptFromContent(string htmlContent, int maxLength)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return string.Empty;

        // Strip HTML tags
        var text = Regex.Replace(htmlContent, "<[^>]+>", " ");
        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= maxLength)
            return text;

        // Truncate at word boundary
        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
            truncated = truncated.Substring(0, lastSpace);

        return truncated + "...";
    }
}

/// <summary>
/// Lightweight DTO for article lists.
/// </summary>
public record WikiArticleListItemDto
{
    public ShortGuid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string Excerpt { get; init; } = string.Empty;
    public bool IsPublished { get; init; }
    public bool IsPinned { get; init; }
    public int ViewCount { get; init; }
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public string? CreatorName { get; init; }

    public static WikiArticleListItemDto MapFrom(WikiArticle article)
    {
        var excerpt = !string.IsNullOrEmpty(article.Excerpt)
            ? article.Excerpt
            : GenerateExcerpt(article.Content, 150);

        return new WikiArticleListItemDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            Category = article.Category,
            Excerpt = excerpt,
            IsPublished = article.IsPublished,
            IsPinned = article.IsPinned,
            ViewCount = article.ViewCount,
            CreationTime = article.CreationTime,
            LastModificationTime = article.LastModificationTime,
            CreatorName = article.CreatorUser != null
                ? $"{article.CreatorUser.FirstName} {article.CreatorUser.LastName}".Trim()
                : null,
        };
    }

    private static string GenerateExcerpt(string htmlContent, int maxLength)
    {
        if (string.IsNullOrEmpty(htmlContent))
            return string.Empty;

        var text = Regex.Replace(htmlContent, "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= maxLength)
            return text;

        var truncated = text.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
            truncated = truncated.Substring(0, lastSpace);

        return truncated + "...";
    }
}
