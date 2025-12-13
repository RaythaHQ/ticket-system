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

namespace App.Web.Areas.Admin.Pages.Priorities;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public CreatePriorityForm Form { get; set; } = new();

    public List<SelectListItem> ColorOptions { get; set; } = GetColorOptions();

    public IActionResult OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Ticket Priorities",
                RouteName = RouteNames.TicketPriorities.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create Priority",
                RouteName = RouteNames.TicketPriorities.Create,
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
            return Page();
        }

        var response = await Mediator.Send(new CreateTicketPriority.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            ColorName = Form.ColorName,
            IsDefault = Form.IsDefault
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Priority created successfully.");
            return RedirectToPage(RouteNames.TicketPriorities.Index);
        }

        SetErrorMessage(response.GetErrors());
        ColorOptions = GetColorOptions();
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

    public class CreatePriorityForm
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

        [Display(Name = "Set as default priority for new tickets")]
        public bool IsDefault { get; set; }
    }
}

