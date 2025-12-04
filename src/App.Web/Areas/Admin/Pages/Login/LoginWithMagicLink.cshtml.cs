using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.AuthenticationSchemes.Queries;
using App.Web.Areas.Admin.Pages.Shared;

namespace App.Web.Areas.Admin.Pages.Login;

public class LoginWithMagicLink : BaseAdminLoginPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet(string returnUrl = null)
    {
        var response = await Mediator.Send(
            new GetAuthenticationSchemes.Query
            {
                IsEnabledForAdmins = true,
                PageSize = int.MaxValue,
            }
        );

        if (OnlyHasSingleSignOnEnabled(response.Result))
        {
            var singleSignOnScheme = response.Result.Items.First();
            return RedirectToPage(
                RouteNames.Login.LoginWithSso,
                new { developerName = singleSignOnScheme.DeveloperName, returnUrl }
            );
        }

        ViewData["returnUrl"] = returnUrl;
        if (BuiltInAuthIsEmailAndPasswordOnly(response.Result))
            return RedirectToPage(RouteNames.Login.LoginWithEmailAndPassword, new { returnUrl });

        AuthenticationSchemes = response.Result.Items.Select(
            p => new LoginAuthenticationSchemeChoiceItemViewModel
            {
                DeveloperName = p.DeveloperName,
                AuthenticationSchemeType = p.AuthenticationSchemeType,
                LoginButtonText = p.LoginButtonText,
            }
        );

        Form = new FormModel();
        return Page();
    }

    public async Task<IActionResult> OnPost(string returnUrl = null)
    {
        var response = await Mediator.Send(
            new App.Application.Login.Commands.BeginLoginWithMagicLink.Command
            {
                EmailAddress = Form.EmailAddress,
                ReturnUrl = returnUrl,
            }
        );

        if (response.Success)
        {
            return RedirectToPage(RouteNames.Login.LoginWithMagicLinkSent);
        }
        else
        {
            var authSchemes = await Mediator.Send(
                new GetAuthenticationSchemes.Query { IsEnabledForAdmins = true, PageSize = 1000 }
            );

            AuthenticationSchemes = authSchemes.Result.Items.Select(
                p => new LoginAuthenticationSchemeChoiceItemViewModel
                {
                    DeveloperName = p.DeveloperName,
                    AuthenticationSchemeType = p.AuthenticationSchemeType,
                    LoginButtonText = p.LoginButtonText,
                }
            );

            SetErrorMessage(response.GetErrors());

            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Your email address")]
        public string EmailAddress { get; set; }
    }
}
