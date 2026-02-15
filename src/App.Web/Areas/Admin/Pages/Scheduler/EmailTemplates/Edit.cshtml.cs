using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.SchedulerAdmin.Commands;
using App.Application.SchedulerAdmin.DTOs;
using App.Domain.Entities;
using CSharpVitamins;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Admin.Pages.Scheduler.EmailTemplates;

[Authorize(Policy = BuiltInSystemPermission.MANAGE_SCHEDULER_SYSTEM_PERMISSION)]
public class Edit : BaseAdminPageModel
{
    private readonly IAppDbContext _db;

    public Edit(IAppDbContext db)
    {
        _db = db;
    }

    public string TemplateTypeName { get; set; } = string.Empty;

    [BindProperty]
    public EditEmailTemplateViewModel Form { get; set; } = new();

    public static readonly List<string> MergeVariables = new()
    {
        "AppointmentCode",
        "MeetingLink",
        "AppointmentType",
        "AppointmentMode",
        "DateTime",
        "Duration",
        "StaffName",
        "StaffEmail",
        "ContactName",
        "ContactEmail",
        "ContactZipcode",
        "Notes",
    };

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        var templateId = ((ShortGuid)id).Guid;
        var template = await _db.SchedulerEmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
        {
            SetErrorMessage("Email template not found.");
            return RedirectToPage(RouteNames.Scheduler.EmailTemplates.Index);
        }

        TemplateTypeName = FormatTemplateType(template.TemplateType);

        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "Scheduler Email Templates",
                RouteName = RouteNames.Scheduler.EmailTemplates.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = TemplateTypeName,
                RouteName = RouteNames.Scheduler.EmailTemplates.Edit,
                IsActive = true,
                RouteValues = new Dictionary<string, string> { { "id", id } },
            }
        );

        Form = new EditEmailTemplateViewModel
        {
            Id = id,
            Subject = template.Subject ?? string.Empty,
            Content = template.Content,
        };

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        var response = await Mediator.Send(new UpdateSchedulerEmailTemplate.Command
        {
            TemplateId = Form.Id,
            Subject = Form.Subject,
            Content = Form.Content,
        }, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Email template updated successfully.");
            return RedirectToPage(RouteNames.Scheduler.EmailTemplates.Index);
        }

        SetErrorMessage(response.GetErrors());

        // Reload template type name for display
        var templateId = ((ShortGuid)Form.Id).Guid;
        var template = await _db.SchedulerEmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
        TemplateTypeName = template != null ? FormatTemplateType(template.TemplateType) : "Email Template";

        return Page();
    }

    private static string FormatTemplateType(string templateType) => templateType switch
    {
        "confirmation" => "Appointment Confirmation",
        "reminder" => "Appointment Reminder",
        "post_meeting" => "Post-Meeting Follow-up",
        _ => templateType,
    };

    public record EditEmailTemplateViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Content")]
        public string Content { get; set; } = string.Empty;
    }
}
