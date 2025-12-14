using System.Linq.Expressions;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;

namespace App.Application.Imports;

public record ImportJobDto : BaseAuditableEntityDto
{
    public ShortGuid RequesterUserId { get; init; }
    public string? RequesterName { get; init; }
    public DateTime RequestedAt { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string EntityTypeLabel { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
    public bool IsDryRun { get; init; }
    public ShortGuid SourceMediaItemId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string? ProgressStage { get; init; }
    public int? ProgressPercent { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int TotalRows { get; init; }
    public int RowsProcessed { get; init; }
    public int RowsInserted { get; init; }
    public int RowsUpdated { get; init; }
    public int RowsSkipped { get; init; }
    public int RowsWithErrors { get; init; }
    public ShortGuid? ErrorMediaItemId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCleanedUp { get; init; }
    public ShortGuid? BackgroundTaskId { get; init; }

    public static Expression<Func<ImportJob, ImportJobDto>> GetProjection()
    {
        return entity => GetProjection(entity);
    }

    public static ImportJobDto GetProjection(ImportJob entity)
    {
        if (entity == null)
            return null!;

        return new ImportJobDto
        {
            Id = entity.Id,
            RequesterUserId = entity.RequesterUserId,
            RequesterName = entity.Requester?.FullName,
            RequestedAt = entity.RequestedAt,
            EntityType = entity.EntityType.DeveloperName,
            EntityTypeLabel = entity.EntityType.Label,
            Mode = entity.Mode.DeveloperName,
            ModeLabel = entity.Mode.Label,
            IsDryRun = entity.IsDryRun,
            SourceMediaItemId = entity.SourceMediaItemId,
            Status = entity.Status.DeveloperName,
            StatusLabel = entity.Status.Label,
            ProgressStage = entity.ProgressStage,
            ProgressPercent = entity.ProgressPercent,
            ErrorMessage = entity.ErrorMessage,
            CompletedAt = entity.CompletedAt,
            TotalRows = entity.TotalRows,
            RowsProcessed = entity.RowsProcessed,
            RowsInserted = entity.RowsInserted,
            RowsUpdated = entity.RowsUpdated,
            RowsSkipped = entity.RowsSkipped,
            RowsWithErrors = entity.RowsWithErrors,
            ErrorMediaItemId = entity.ErrorMediaItemId,
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
