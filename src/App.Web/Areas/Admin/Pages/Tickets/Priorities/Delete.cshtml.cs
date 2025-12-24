using App.Application.TicketConfig.Commands;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Priorities;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteTicketPriority.Command { Id = id };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Ticket priority has been deleted.");
            return RedirectToPage(RouteNames.TicketPriorities.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error deleting this priority",
                response.GetErrors()
            );
            return RedirectToPage(RouteNames.TicketPriorities.Edit, new { id });
        }
    }
}

