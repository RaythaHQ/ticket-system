using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Exports;

public record ExportJobDto : BaseAuditableEntityDto
{
    public ShortGuid RequesterUserId { get; init; }
    public string? RequesterName { get; init; }
    public DateTime RequestedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? ProgressStage { get; init; }
    public int? ProgressPercent { get; init; }
    public int? RowCount { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? CompletedAt { get; init; }
    public ShortGuid? MediaItemId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCleanedUp { get; init; }
    public ShortGuid? BackgroundTaskId { get; init; }

    public static Expression<Func<ExportJob, ExportJobDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static ExportJobDto GetProjection(ExportJob entity)
    {
        if (entity == null)
            return null!;

        return new ExportJobDto
        {
            Id = entity.Id,
            RequesterUserId = entity.RequesterUserId,
            RequesterName = entity.Requester?.FullName,
            RequestedAt = entity.RequestedAt,
            Status = entity.Status.DeveloperName,
            ProgressStage = entity.ProgressStage,
            ProgressPercent = entity.ProgressPercent,
            RowCount = entity.RowCount,
            ErrorMessage = entity.ErrorMessage,
            CompletedAt = entity.CompletedAt,
            MediaItemId = entity.MediaItemId,
            ExpiresAt = entity.ExpiresAt,
            IsCleanedUp = entity.IsCleanedUp,
            BackgroundTaskId = entity.BackgroundTaskId,
            CreationTime = entity.CreationTime,
            LastModificationTime = entity.LastModificationTime,
            CreatorUserId = entity.CreatorUserId,
            LastModifierUserId = entity.LastModifierUserId,
        };
    }
}

