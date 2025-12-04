using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Utils;
using App.Application.OrganizationSettings.Queries;
using App.Domain.Entities;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;

namespace App.Web.Areas.Admin.Pages.Configuration;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SYSTEM_SETTINGS_PERMISSION)]
public class Index : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public IDictionary<string, string> AvailableTimeZones
    {
        get { return DateTimeExtensions.GetTimeZoneDisplayNames(); }
    }

    public IDictionary<string, string> AvailableDateFormats
    {
        get
        {
            var dateFormats = new Dictionary<string, string>();
            foreach (var dF in DateTimeExtensions.GetDateFormats())
            {
                dateFormats.Add(dF, dF);
            }
            return dateFormats;
        }
    }

    public async Task<IActionResult> OnGet()
    {
        // Set breadcrumbs for navigation
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
                Label = "Configuration",
                RouteName = RouteNames.Configuration.Index,
                IsActive = true,
            }
        );

        var input = new GetOrganizationSettings.Query();
        var response = await Mediator.Send(input);
        Form = new FormModel
        {
            OrganizationName = response.Result.OrganizationName,
            WebsiteUrl = response.Result.WebsiteUrl,
            DateFormat = response.Result.DateFormat,
            TimeZone = response.Result.TimeZone,
            SmtpDefaultFromAddress = response.Result.SmtpDefaultFromAddress,
            SmtpDefaultFromName = response.Result.SmtpDefaultFromName,
        };
        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var input = new App.Application.OrganizationSettings.Commands.EditConfiguration.Command
        {
            OrganizationName = Form.OrganizationName,
            TimeZone = Form.TimeZone,
            DateFormat = Form.DateFormat,
            WebsiteUrl = Form.WebsiteUrl,
            SmtpDefaultFromAddress = Form.SmtpDefaultFromAddress,
            SmtpDefaultFromName = Form.SmtpDefaultFromName,
        };
        var response = await Mediator.Send(input);

        if (response.Success)
        {
            SetSuccessMessage("Configuration has been updated successfully.");
            return RedirectToPage(RouteNames.Configuration.Index);
        }
        else
        {
            SetErrorMessage(
                "There was an error attempting to save the configuration. See the error below.",
                response.GetErrors()
            );
            return Page();
        }
    }

    public record FormModel
    {
        [Display(Name = "Organization name")]
        public string OrganizationName { get; set; }

        [Display(Name = "Website url")]
        public string WebsiteUrl { get; set; }

        [Display(Name = "Time zone")]
        public string TimeZone { get; set; }

        [Display(Name = "Date format")]
        public string DateFormat { get; set; }

        [Display(Name = "Default reply-to email address")]
        public string SmtpDefaultFromAddress { get; set; }

        [Display(Name = "Default reply-to name")]
        public string SmtpDefaultFromName { get; set; }
    }
}
