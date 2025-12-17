using System.ComponentModel.DataAnnotations;
using App.Application.Login.Commands;
using App.Application.NotificationPreferences;
using App.Application.NotificationPreferences.Commands;
using App.Application.NotificationPreferences.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.Profile;

public class Index : BaseAdminPageModel
{
    [BindProperty]
    public FormModel Form { get; set; }

    public List<NotificationPreferenceDto> NotificationPreferences { get; set; } = new();
    public bool PlaySoundOnNotification { get; set; } = true;

    public async Task<IActionResult> OnGet()
    {
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Profile",
                RouteName = RouteNames.Profile.Index,
                IsActive = true,
                Icon = SidebarIcons.Users,
            }
        );

        Form = new FormModel()
        {
            FirstName = CurrentUser.FirstName,
            LastName = CurrentUser.LastName,
            EmailAddress = CurrentUser.EmailAddress,
        };

        // Load notification preferences
        if (CurrentUser.UserId?.Guid != null)
        {
            var prefsResponse = await Mediator.Send(
                new GetNotificationPreferences.Query
                {
                    StaffAdminId = CurrentUser.UserId.Value.Guid,
                }
            );
            NotificationPreferences = prefsResponse.Result;

            // Load sound preference
            var userResponse = await Mediator.Send(
                new App.Application.Users.Queries.GetUserById.Query
                {
                    Id = CurrentUser.UserId.Value.ToString(),
                }
            );
            PlaySoundOnNotification = userResponse.Result?.PlaySoundOnNotification ?? true;
        }

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

    public async Task<IActionResult> OnPostUpdateNotifications(
        [FromForm] Dictionary<string, string> emailPrefs,
        [FromForm] Dictionary<string, string> inAppPrefs,
        [FromForm] bool playSoundOnNotification,
        CancellationToken cancellationToken
    )
    {
        if (CurrentUser.UserId?.Guid == null)
            return RedirectToPage(RouteNames.Profile.Index);

        // Get all supported event types
        var allEventTypes = App.Domain.ValueObjects.NotificationEventType.SupportedTypes.ToList();

        // Build preferences list - checked checkboxes will be in the dictionary
        // Unchecked checkboxes won't be in the dictionary at all
        var preferences = allEventTypes
            .Select(eventType => new UpdateNotificationPreferences.PreferenceUpdate
            {
                EventType = eventType.DeveloperName,
                EmailEnabled = emailPrefs?.ContainsKey(eventType.DeveloperName) ?? false,
                InAppEnabled = inAppPrefs?.ContainsKey(eventType.DeveloperName) ?? false,
                WebhookEnabled = false,
            })
            .ToList();

        var response = await Mediator.Send(
            new UpdateNotificationPreferences.Command
            {
                StaffAdminId = CurrentUser.UserId.Value.Guid,
                Preferences = preferences,
                PlaySoundOnNotification = playSoundOnNotification,
            },
            cancellationToken
        );

        if (response.Success)
        {
            SetSuccessMessage("Notification preferences updated successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Profile.Index);
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
