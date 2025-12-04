using System.ComponentModel.DataAnnotations;
using App.Application.Login.Commands;
using App.Web.Areas.Public.Pages.Shared;
using App.Web.Areas.Public.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Public.Pages.Profile;

public class Index : BasePublicPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Profile",
                RouteName = RouteNames.Profile.Index,
                IsActive = true,
            }
        );

        Form = new FormModel()
        {
            FirstName = CurrentUser.FirstName,
            LastName = CurrentUser.LastName,
            EmailAddress = CurrentUser.EmailAddress,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var response = await Mediator.Send(
            new ChangeProfile.Command
            {
                Id = CurrentUser.UserId.Value,
                FirstName = Form.FirstName,
                LastName = Form.LastName,
            }
        );

        if (response.Success)
        {
            SetSuccessMessage("Profile changed successfully.");
            return RedirectToPage(RouteNames.Profile.Index);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "First name")]
        public string FirstName { get; set; }

        [Display(Name = "Last name")]
        public string LastName { get; set; }

        [Display(Name = "Email address")]
        public string EmailAddress { get; set; }
    }
}
