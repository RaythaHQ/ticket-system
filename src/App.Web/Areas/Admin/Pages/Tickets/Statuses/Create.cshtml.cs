using System.ComponentModel.DataAnnotations;
using App.Application.TicketConfig.Commands;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace App.Web.Areas.Admin.Pages.Statuses;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public CreateStatusForm Form { get; set; } = new();

    public List<SelectListItem> ColorOptions { get; set; } = GetColorOptions();
    public List<SelectListItem> StatusTypeOptions { get; set; } = GetStatusTypeOptions();

    public IActionResult OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Ticket Statuses",
                RouteName = RouteNames.TicketStatuses.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create Status",
                RouteName = RouteNames.TicketStatuses.Create,
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

        var response = await Mediator.Send(new CreateTicketStatus.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            ColorName = Form.ColorName,
            StatusType = Form.StatusType
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Status created successfully.");
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
            new("Dark", "dark")
        };
    }

    private static List<SelectListItem> GetStatusTypeOptions()
    {
        return new List<SelectListItem>
        {
            new("Open - Ticket is active", TicketStatusType.OPEN),
            new("Closed - Ticket is complete", TicketStatusType.CLOSED)
        };
    }

    public class CreateStatusForm
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "Label")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [RegularExpression("^[a-z][a-z0-9_]*$", ErrorMessage = "Must start with a lowercase letter and contain only lowercase letters, numbers, and underscores.")]
        [Display(Name = "Developer Name")]
        public string DeveloperName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Color")]
        public string ColorName { get; set; } = "secondary";

        [Required]
        [Display(Name = "Status Type")]
        public string StatusType { get; set; } = TicketStatusType.OPEN;
    }
}

