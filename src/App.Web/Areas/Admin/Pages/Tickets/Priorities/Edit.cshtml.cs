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

namespace App.Web.Areas.Admin.Pages.Priorities;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public EditPriorityForm Form { get; set; } = new();

    public List<SelectListItem> ColorOptions { get; set; } = GetColorOptions();
    public bool IsBuiltIn { get; set; }
    public string DeveloperName { get; set; } = string.Empty;

    public async Task<IActionResult> OnGet(ShortGuid id, CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new GetTicketPriorityById.Query { Id = id }, cancellationToken);

        if (response.Result == null)
        {
            SetErrorMessage("Priority not found.");
            return RedirectToPage(RouteNames.TicketPriorities.Index);
        }

        var priority = response.Result;
        Form = new EditPriorityForm
        {
            Id = priority.Id,
            Label = priority.Label,
            ColorName = priority.ColorName,
            IsDefault = priority.IsDefault
        };

        IsBuiltIn = priority.IsBuiltIn;
        DeveloperName = priority.DeveloperName;

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Ticket Priorities",
                RouteName = RouteNames.TicketPriorities.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = $"Edit: {priority.Label}",
                RouteName = RouteNames.TicketPriorities.Edit,
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

        var response = await Mediator.Send(new UpdateTicketPriority.Command
        {
            Id = Form.Id,
            Label = Form.Label,
            ColorName = Form.ColorName,
            IsDefault = Form.IsDefault
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Priority updated successfully.");
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

    public class EditPriorityForm
    {
        public ShortGuid Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Label")]
        public string Label { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Color")]
        public string ColorName { get; set; } = "secondary";

        [Display(Name = "Set as default priority for new tickets")]
        public bool IsDefault { get; set; }
    }
}

