using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Roles.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Roles;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Create : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Settings",
                RouteName = RouteNames.Configuration.Index,
                IsActive = false,
                Icon = SidebarIcons.Settings,
            },
            new BreadcrumbNode
            {
                Label = "Roles",
                RouteName = RouteNames.Roles.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create role",
                RouteName = RouteNames.Roles.Create,
                IsActive = true,
            }
        );

        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new FormModel.SystemPermissionCheckboxItemViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = false,
            }
        );

        Form = new FormModel
        {
            SystemPermissions = systemPermissions.ToArray(),
        };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new CreateRole.Command
        {
            Label = Form.Label,
            DeveloperName = Form.DeveloperName,
            SystemPermissions = Form
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was created successfully.");
            return RedirectToPage(RouteNames.Roles.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to create this role. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Label")]
        public string Label { get; set; }

        [Display(Name = "Developer name")]
        public string DeveloperName { get; set; }

        public SystemPermissionCheckboxItemViewModel[] SystemPermissions { get; set; }

        public class SystemPermissionCheckboxItemViewModel
        {
            public string DeveloperName { get; set; }
            public bool Selected { get; set; }
            public string Label { get; set; }
        }
    }
}
