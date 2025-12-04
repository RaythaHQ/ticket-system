using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;

namespace App.Application.MediaItems;

public record MediaItemDto : BaseAuditableEntityDto
{
    public long Length { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string FileStorageProvider { get; init; } = string.Empty;
    public string ObjectKey { get; init; } = string.Empty;

    public static Expression<Func<MediaItem, MediaItemDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static MediaItemDto GetProjection(MediaItem entity)
    {
        if (entity == null)
            return null;

        return new MediaItemDto
        {
            Id = entity.Id,
            Length = entity.Length,
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            FileStorageProvider = entity.FileStorageProvider,
            ObjectKey = entity.ObjectKey,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            CreatorUserId = entity.CreatorUserId,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}

