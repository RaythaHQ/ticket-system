namespace App.Domain.Entities;

public class MediaItem : BaseAuditableEntity
{
    public long Length { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string FileStorageProvider { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
}

