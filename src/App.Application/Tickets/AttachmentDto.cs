using CSharpVitamins;

namespace App.Application.Tickets;

/// <summary>
/// DTO for ticket attachment data.
/// </summary>
public record TicketAttachmentDto
{
    public ShortGuid Id { get; init; }
    public long TicketId { get; init; }
    public ShortGuid MediaItemId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string ObjectKey { get; init; } = string.Empty;
    public ShortGuid UploadedByUserId { get; init; }
    public string UploadedByUserName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public string FileSizeDisplay => FormatFileSize(SizeBytes);

    /// <summary>
    /// Gets the file extension from the filename.
    /// </summary>
    public string FileExtension => Path.GetExtension(FileName)?.TrimStart('.').ToUpperInvariant() ?? string.Empty;

    /// <summary>
    /// Determines if this is an image file based on content type.
    /// </summary>
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Determines if this is a PDF file based on content type.
    /// </summary>
    public bool IsPdf => ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

