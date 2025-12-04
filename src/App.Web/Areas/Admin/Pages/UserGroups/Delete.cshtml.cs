using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.UserGroups.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.UserGroups;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_USERS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var response = await Mediator.Send(new DeleteUserGroup.Command { Id = id });
        if (response.Success)
        {
            SetSuccessMessage($"User group has been deleted.");
            return RedirectToPage(RouteNames.UserGroups.Index);
        }
        else
        {
            SetErrorMessage("There was a problem deleting this user group", response.GetErrors());
            return RedirectToPage(RouteNames.UserGroups.Edit, new { id = id });
        }
    }
}
