using System.ComponentModel.DataAnnotations;
using App.Application.TicketConfig.Commands;
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

namespace App.Web.Areas.Admin.Pages.Statuses;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public EditStatusForm Form { get; set; } = new();

    public List<SelectListItem> ColorOptions { get; set; } = GetColorOptions();
    public List<SelectListItem> StatusTypeOptions { get; set; } = GetStatusTypeOptions();
    public bool IsBuiltIn { get; set; }
    public bool IsFirst { get; set; }
    public string DeveloperName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGet(ShortGuid id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(
            new GetTicketStatusById.Query { Id = id },
            cancellationToken
        );

        if (response.Result == null)
        {
            SetErrorMessage("Status not found.");
            return RedirectToPage(RouteNames.TicketStatuses.Index);
        }

        var status = response.Result;
        Form = new EditStatusForm
        {
            Id = status.Id,
            Label = status.Label,
            ColorName = status.ColorName,
            StatusType = status.StatusType,
        };

        IsBuiltIn = status.IsBuiltIn;
        IsFirst = status.SortOrder == 1;
        DeveloperName = status.DeveloperName;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Ticket Statuses",
                RouteName = RouteNames.TicketStatuses.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = $"Edit: {status.Label}",
                RouteName = RouteNames.TicketStatuses.Edit,
                IsActive = true,
            }
        );

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ColorOptions = GetColorOptions();
            StatusTypeOptions = GetStatusTypeOptions();
            return Page();
        }

        var response = await Mediator.Send(
            new UpdateTicketStatus.Command
            {
                Id = Form.Id,
                Label = Form.Label,
                ColorName = Form.ColorName,
                StatusType = Form.StatusType,
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("Status updated successfully.");
            return RedirectToPage(RouteNames.TicketStatuses.Index);
        }

        SetErrorMessage(response.GetErrors());
        ColorOptions = GetColorOptions();
        StatusTypeOptions = GetStatusTypeOptions();
        return Page();
    }

    private static List<SelectListItem> GetColorOptions()
    {
        return new List<SelectListItem>
        {
            new("Danger (Red)", "danger"),
            new("Warning (Yellow/Orange)", "warning"),
            new("Primary (Blue)", "primary"),
            new("Success (Green)", "success"),
            new("Info (Light Blue)", "info"),
            new("Secondary (Gray)", "secondary"),
            new("Light", "light"),
            new("Dark", "dark"),
        };
    }

    private static List<SelectListItem> GetStatusTypeOptions()
    {
        return new List<SelectListItem>
        {
            new("Open - Ticket is active", TicketStatusType.OPEN),
            new("Closed - Ticket is complete", TicketStatusType.CLOSED),
        };
    }

    public class EditStatusForm
    {
        public ShortGuid Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Label")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Color")]
        public string ColorName { get; set; } = "secondary";

        [Required]
        [Display(Name = "Status Type")]
        public string StatusType { get; set; } = TicketStatusType.OPEN;
    }
}
