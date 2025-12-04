using System.ComponentModel.DataAnnotations;
using App.Web.Areas.Public.Pages.Shared;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Login;

public class ForgotPassword : BasePublicLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (!CurrentOrganization.EmailAndPasswordIsEnabledForUsers)
        {
            SetErrorMessage("Authentication scheme disabled for public users");
            return new ForbidResult();
        }
        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var response = await Mediator.Send(
            new App.Application.Login.Commands.BeginForgotPassword.Command
            {
                EmailAddress = Form.EmailAddress,
            }
        );
        if (response.Success)
        {
            return RedirectToPage(RouteNames.Login.ForgotPasswordSent);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public record FormModel
    {
        [Display(Name = "Your email address")]
        public string EmailAddress { get; set; }
    }
}
