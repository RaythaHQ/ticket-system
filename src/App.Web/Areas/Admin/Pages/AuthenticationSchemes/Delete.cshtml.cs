using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.AuthenticationSchemes.Commands;
using App.Domain.Entities;
using App.Infrastructure.Services;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.AuthenticationSchemes;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Delete : BaseAdminPageModel
{
    private readonly ICachedOrganizationService _cachedOrganizationService;

    public Delete(ICachedOrganizationService cachedOrganizationService)
    {
        _cachedOrganizationService = cachedOrganizationService;
    }
    public async Task<IActionResult> OnPost(string id)
    {
        var input = new DeleteAuthenticationScheme.Command { Id = id };
        var response = await Mediator.Send(input);
        if (response.Success)
        {
            _cachedOrganizationService.InvalidateAuthenticationSchemesCache();
            SetSuccessMessage($"Authentication scheme has been deleted.");
            return RedirectToPage(RouteNames.AuthenticationSchemes.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error deleting this authentication scheme",
                response.GetErrors()
            );
            return RedirectToPage(RouteNames.AuthenticationSchemes.Edit, new { id });
        }
    }
}
