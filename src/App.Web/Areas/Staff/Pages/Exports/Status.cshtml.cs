using App.Application.Exports;
using App.Application.Exports.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Staff.Pages.Exports;

/// <summary>
/// Page model for displaying export job status.
/// </summary>
public class Status : BaseStaffPageModel
{
    /// <summary>
    /// The export job being tracked.
    /// </summary>
    public ExportJobDto? ExportJob { get; set; }

    /// <summary>
    /// Whether the current user can download the export.
    /// </summary>
    public bool CanDownload { get; set; }

    /// <summary>
    /// The download URL for the export file.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Whether the export is complete and ready for download.
    /// </summary>
    public bool IsComplete => ExportJob?.Status == ExportJobStatus.COMPLETED;

    /// <summary>
    /// Whether the export has failed.
    /// </summary>
    public bool IsFailed => ExportJob?.Status == ExportJobStatus.FAILED;

    /// <summary>
    /// Whether the export is still in progress.
    /// </summary>
    public bool IsInProgress => ExportJob?.Status is ExportJobStatus.QUEUED or ExportJobStatus.RUNNING;

    /// <summary>
    /// Handles GET requests to display export status.
    /// </summary>
    public async Task<IActionResult> OnGetAsync(
        ShortGuid id,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Export Status";
        ViewData["ActiveMenu"] = "Tickets";

        var response = await Mediator.Send(
            new GetExportJobById.Query { Id = id },
            cancellationToken);

        ExportJob = response.Result;

        if (ExportJob == null)
        {
            SetErrorMessage("Export job not found.");
            return RedirectToPage("/Tickets/Index");
        }

        // Check download permissions: must be Admin AND have ImportExportTickets permission
        CanDownload = CurrentUser.IsAdmin &&
            CurrentUser.SystemPermissions.Contains(
                Domain.Entities.BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION);

        // Generate download URL if media item exists
        if (ExportJob.MediaItemId.HasValue)
        {
            DownloadUrl = RelativeUrlBuilder.MediaRedirectToFileUrlById(
                ExportJob.MediaItemId.Value.ToString());
        }

        return Page();
    }

    /// <summary>
    /// Handles POST to retry a failed export.
    /// </summary>
    public async Task<IActionResult> OnPostRetryAsync(
        ShortGuid id,
        CancellationToken cancellationToken = default)
    {
        var command = new Application.Exports.Commands.RetryExportJob.Command
        {
            OriginalExportJobId = id
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Export retry queued successfully.");
            return RedirectToPage(new { id = response.Result });
        }
        else
        {
            SetErrorMessage(!string.IsNullOrEmpty(response.Error) ? response.Error : "Failed to retry export.");
            return RedirectToPage(new { id });
        }
    }

    /// <summary>
    /// Gets export status data for AJAX polling.
    /// </summary>
    public async Task<IActionResult> OnGetStatusAsync(
        ShortGuid id,
        CancellationToken cancellationToken = default)
    {
        var response = await Mediator.Send(
            new GetExportJobById.Query { Id = id },
            cancellationToken);

        if (response.Result == null)
        {
            return NotFound();
        }

        var job = response.Result;
        return new JsonResult(new
        {
            status = job.Status,
            progressStage = job.ProgressStage,
            progressPercent = job.ProgressPercent ?? 0,
            rowCount = job.RowCount,
            isComplete = job.Status == ExportJobStatus.COMPLETED,
            isFailed = job.Status == ExportJobStatus.FAILED,
            mediaItemId = job.MediaItemId?.ToString(),
            errorMessage = job.ErrorMessage,
        });
    }
}

