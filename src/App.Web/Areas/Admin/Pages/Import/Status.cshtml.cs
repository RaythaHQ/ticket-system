using App.Application.Common.Interfaces;
using App.Application.Imports;
using App.Application.Imports.Queries;
using App.Application.MediaItems.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Import;

[Authorize(Policy = BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION)]
public class Status : BaseAdminPageModel
{
    public ImportJobDto ImportJob { get; set; } = null!;
    public string? ErrorFileDownloadUrl { get; set; }

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Import Data",
                RouteName = RouteNames.Import.Index,
                IsActive = false,
                Icon = SidebarIcons.Import,
            },
            new BreadcrumbNode
            {
                Label = "Import Status",
                RouteName = RouteNames.Import.Status,
                IsActive = true,
            }
        );

        try
        {
            var response = await Mediator.Send(
                new GetImportJobById.Query { Id = id },
                cancellationToken
            );
            ImportJob = response.Result;

            // Get error file download URL if available
            if (ImportJob.ErrorMediaItemId.HasValue)
            {
                var mediaItemResponse = await Mediator.Send(
                    new GetMediaItemById.Query { Id = ImportJob.ErrorMediaItemId.Value.ToString() },
                    cancellationToken
                );

                ErrorFileDownloadUrl = await FileStorageProvider.GetDownloadUrlAsync(
                    mediaItemResponse.Result.ObjectKey,
                    DateTime.UtcNow.AddHours(1)
                );
            }

            return Page();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load import job {Id}", id);
            SetErrorMessage("Import job not found.");
            return RedirectToPage(RouteNames.Import.Index);
        }
    }

    public async Task<IActionResult> OnGetRefresh(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await Mediator.Send(
                new GetImportJobById.Query { Id = id },
                cancellationToken
            );
            var job = response.Result;

            string? errorFileUrl = null;
            if (job.ErrorMediaItemId.HasValue)
            {
                var mediaItemResponse = await Mediator.Send(
                    new GetMediaItemById.Query { Id = job.ErrorMediaItemId.Value.ToString() },
                    cancellationToken
                );

                errorFileUrl = await FileStorageProvider.GetDownloadUrlAsync(
                    mediaItemResponse.Result.ObjectKey,
                    DateTime.UtcNow.AddHours(1)
                );
            }

            return new JsonResult(
                new
                {
                    status = job.Status,
                    statusLabel = job.StatusLabel,
                    progressStage = job.ProgressStage,
                    progressPercent = job.ProgressPercent ?? 0,
                    totalRows = job.TotalRows,
                    rowsProcessed = job.RowsProcessed,
                    rowsInserted = job.RowsInserted,
                    rowsUpdated = job.RowsUpdated,
                    rowsSkipped = job.RowsSkipped,
                    rowsWithErrors = job.RowsWithErrors,
                    completedAt = job.CompletedAt?.ToString("MMM dd, yyyy h:mm tt"),
                    errorMessage = job.ErrorMessage,
                    errorFileUrl = errorFileUrl,
                    isDryRun = job.IsDryRun,
                }
            );
        }
        catch
        {
            return new JsonResult(new { error = "Failed to load import status" })
            {
                StatusCode = 404,
            };
        }
    }
}
