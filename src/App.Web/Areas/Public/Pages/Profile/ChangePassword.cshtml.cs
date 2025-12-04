using System.ComponentModel.DataAnnotations;
using App.Application.Login.Commands;
using App.Web.Areas.Public.Pages.Shared;
using App.Web.Areas.Public.Pages.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Profile;

public class ChangePassword : BasePublicPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Profile.Index);
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            SetErrorMessage("Authentication scheme is disabled");
            return RedirectToPage(RouteNames.Profile.Index);
        }

        var response = await Mediator.Send(
            new App.Application.Login.Commands.ChangePassword.Command
            {
                Id = CurrentUser.UserId.Value,
                CurrentPassword = Form.CurrentPassword,
                NewPassword = Form.NewPassword,
                ConfirmNewPassword = Form.ConfirmNewPassword,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Password changed successfully.");
            return RedirectToPage(RouteNames.Profile.ChangePassword);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Your current password")]
        public string CurrentPassword { get; set; }

        [Display(Name = "Your new password")]
        public string NewPassword { get; set; }

        [Display(Name = "Re-type your new password")]
        public string ConfirmNewPassword { get; set; }
    }
}

