using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Roles.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Roles;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteRole.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"Role has been deleted.");
            return RedirectToPage(RouteNames.Roles.Index);
        }
        else
        {
            SetErrorMessage("There was a problem deleting this role", response.GetErrors());
            return RedirectToPage(RouteNames.Roles.Edit, new { id });
        }
    }
}
