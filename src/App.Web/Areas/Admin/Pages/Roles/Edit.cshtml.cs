using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Roles.Commands;
using App.Application.Roles.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Roles;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }
    public bool IsSuperAdmin { get; set; }

    public async Task<IActionResult> OnGet(string id)
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
                Label = "Edit role",
                RouteName = RouteNames.Roles.Edit,
                IsActive = true,
            }
        );

        var response = await Mediator.Send(new GetRoleById.Query { Id = id });

        var systemPermissions = BuiltInSystemPermission.Permissions.Select(
            p => new FormModel.SystemPermissionCheckboxItemViewModel
            {
                DeveloperName = p.DeveloperName,
                Label = p.Label,
                Selected = response.Result.SystemPermissions.Any(c => c == p.DeveloperName),
            }
        );

        Form = new FormModel
        {
            Id = response.Result.Id,
            Label = response.Result.Label,
            DeveloperName = response.Result.DeveloperName,
            SystemPermissions = systemPermissions.ToArray(),
        };

        IsSuperAdmin = BuiltInRole.SuperAdmin.DeveloperName == response.Result.DeveloperName;
        return Page();
    }

    public async Task<IActionResult> OnPost(string id)
    {
        var input = new EditRole.Command
        {
            Id = Form.Id,
            Label = Form.Label,
            SystemPermissions = Form
                .SystemPermissions.Where(p => p.Selected)
                .Select(p => p.DeveloperName),
        };

        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage($"{Form.Label} was updated successfully.");
            return RedirectToPage(RouteNames.Roles.Edit, new { id });
        }
        {
            SetErrorMessage(
                "There was an error attempting to update this role. See the error below.",
                response.GetErrors()
            );
            var currentRole = await Mediator.Send(new GetRoleById.Query { Id = id });
            IsSuperAdmin = BuiltInRole.SuperAdmin.DeveloperName == currentRole.Result.DeveloperName;
            return Page();
        }
    }

    public record FormModel
    {
        public string Id { get; set; }

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
