using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Common.Utils;
using App.Application.Imports.Commands;
using App.Application.MediaItems.Commands;
using App.Application.TicketConfig.Queries;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Web.Areas.Admin.Pages.Import;

[Authorize(Policy = BuiltInSystemPermission.IMPORT_EXPORT_TICKETS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; } = new();

    public List<string> ValidStatuses { get; set; } = new();
    public List<string> ValidPriorities { get; set; } = new();
    public List<SelectListItem> SlaModeOptions { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Import Data",
                RouteName = RouteNames.Import.Index,
                IsActive = true,
                Icon = SidebarIcons.Import,
            }
        );

        await LoadReferenceDataAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(IFormFile? file, CancellationToken cancellationToken)
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Import Data",
                RouteName = RouteNames.Import.Index,
                IsActive = true,
                Icon = SidebarIcons.Import,
            }
        );

        await LoadReferenceDataAsync(cancellationToken);

        if (file == null || file.Length == 0)
        {
            SetErrorMessage("Please select a CSV file to upload.");
            return Page();
        }

        // Validate file type
        if (
            !file.ContentType.Contains("csv")
            && !file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
        )
        {
            SetErrorMessage("Only CSV files are allowed.");
            return Page();
        }

        // Validate file size (30 MB max)
        const long maxFileSize = 30 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            SetErrorMessage("File size exceeds the maximum limit of 30 MB.");
            return Page();
        }

        try
        {
            // Upload file to storage
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            var data = stream.ToArray();

            var idForKey = ShortGuid.NewGuid();
            var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
                idForKey.ToString(),
                file.FileName
            );

            await FileStorageProvider.SaveAndGetDownloadUrlAsync(
                data,
                objectKey,
                file.FileName,
                "text/csv",
                DateTime.UtcNow.AddHours(72)
            );

            // Create MediaItem
            var createMediaItemResponse = await Mediator.Send(
                new CreateMediaItem.Command
                {
                    Id = idForKey,
                    FileName = file.FileName,
                    Length = data.Length,
                    ContentType = "text/csv",
                    FileStorageProvider = FileStorageProvider.GetName(),
                    ObjectKey = objectKey,
                },
                cancellationToken
            );

            if (!createMediaItemResponse.Success)
            {
                SetErrorMessage("Failed to save uploaded file.");
                return Page();
            }

            // Create import job
            var createImportResponse = await Mediator.Send(
                new CreateImportJob.Command
                {
                    EntityType = Form.EntityType,
                    Mode = Form.Mode,
                    IsDryRun = Form.IsDryRun,
                    SlaMode = Form.EntityType == ImportEntityType.TICKETS ? Form.SlaMode : null,
                    SourceMediaItemId = idForKey,
                },
                cancellationToken
            );

            if (createImportResponse.Success)
            {
                var modeLabel = ImportMode.From(Form.Mode).Label;
                var entityLabel = ImportEntityType.From(Form.EntityType).Label;
                var dryRunText = Form.IsDryRun ? " (Dry Run)" : "";
                SetSuccessMessage($"Import job started: {entityLabel} - {modeLabel}{dryRunText}");
                return RedirectToPage(
                    RouteNames.Import.Status,
                    new { id = createImportResponse.Result.ToString() }
                );
            }
            else
            {
                SetErrorMessage("Failed to start import job.", createImportResponse.GetErrors());
                return Page();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting import job");
            SetErrorMessage("An unexpected error occurred. Please try again.");
            return Page();
        }
    }

    private async Task LoadReferenceDataAsync(CancellationToken cancellationToken)
    {
        var statusesResponse = await Mediator.Send(
            new GetTicketStatuses.Query(),
            cancellationToken
        );
        ValidStatuses = statusesResponse.Result.Items.Select(s => s.DeveloperName).ToList();

        var prioritiesResponse = await Mediator.Send(
            new GetTicketPriorities.Query(),
            cancellationToken
        );
        ValidPriorities = prioritiesResponse.Result.Items.Select(p => p.DeveloperName).ToList();

        // SLA mode options for ticket imports
        SlaModeOptions = ImportSlaMode
            .SupportedTypes.Select(m => new SelectListItem
            {
                Value = m.DeveloperName,
                Text = m.Label,
                Selected = m.DeveloperName == ImportSlaMode.DO_NOT_APPLY,
            })
            .ToList();
    }

    public class FormModel
    {
        [Required(ErrorMessage = "Please select what to import")]
        [Display(Name = "Import Type")]
        public string EntityType { get; set; } = ImportEntityType.CONTACTS;

        [Required(ErrorMessage = "Please select an import mode")]
        [Display(Name = "Import Mode")]
        public string Mode { get; set; } = ImportMode.UPSERT;

        [Display(Name = "Dry Run (Validate Only)")]
        public bool IsDryRun { get; set; } = false;

        [Display(Name = "SLA Rules")]
        public string SlaMode { get; set; } = ImportSlaMode.DO_NOT_APPLY;
    }
}
