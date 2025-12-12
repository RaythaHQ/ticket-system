using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.SlaRules;
using App.Application.SlaRules.Commands;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;
using App.Web.Areas.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Admin.Pages.SlaRules;

/// <summary>
/// Page model for creating a new SLA rule.
/// </summary>
public class Create : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Create(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [BindProperty]
    public CreateSlaRuleViewModel Form { get; set; } = new();

    public IActionResult OnGet()
    {
        if (!_permissionService.CanManageTickets())
            return Forbid();

        // Set breadcrumbs for navigation
        SetBreadcrumbs(
            new BreadcrumbNode
            {
                Label = "SLA Rules",
                RouteName = RouteNames.SlaRules.Index,
                IsActive = false,
            },
            new BreadcrumbNode
            {
                Label = "Create SLA rule",
                RouteName = RouteNames.SlaRules.Create,
                IsActive = true,
            }
        );

        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTickets())
            return Forbid();

        if (!ModelState.IsValid)
            return Page();

        var targetMinutes = CalculateMinutes(Form.TargetResolutionValue, Form.TargetResolutionUnit);

        var conditions = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(Form.ConditionPriority))
            conditions["priority"] = Form.ConditionPriority;
        if (!string.IsNullOrEmpty(Form.ConditionCategory))
            conditions["category"] = Form.ConditionCategory;

        BusinessHoursConfig? businessHours = null;
        if (Form.BusinessHoursEnabled)
        {
            businessHours = new BusinessHoursConfig
            {
                Workdays = new List<int> { 1, 2, 3, 4, 5 }, // Mon-Fri
                StartTime = Form.BusinessHoursStart ?? "08:00",
                EndTime = Form.BusinessHoursEnd ?? "18:00",
            };
        }

        var command = new CreateSlaRule.Command
        {
            Name = Form.Name,
            Description = Form.Description,
            Conditions = conditions,
            TargetResolutionMinutes = targetMinutes,
            BusinessHoursEnabled = Form.BusinessHoursEnabled,
            BusinessHoursConfig = businessHours,
            Priority = Form.Priority,
            BreachBehavior = new BreachBehavior
            {
                UiMarkers = true,
                NotifyAssignee = Form.NotifyAssignee,
            },
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"SLA rule '{Form.Name}' created successfully.");
            return RedirectToPage(RouteNames.SlaRules.Index);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    private int CalculateMinutes(int value, string unit)
    {
        return unit switch
        {
            "minutes" => value,
            "hours" => value * 60,
            "days" => value * 1440,
            _ => value * 60,
        };
    }

    public record CreateSlaRuleViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Target Resolution")]
        [Range(1, 10000)]
        public int TargetResolutionValue { get; set; } = 4;

        public string TargetResolutionUnit { get; set; } = "hours";

        [Display(Name = "Priority (lower = evaluated first)")]
        [Range(0, 1000)]
        public int Priority { get; set; } = 0;

        [Display(Name = "Use Business Hours")]
        public bool BusinessHoursEnabled { get; set; }

        public string? BusinessHoursStart { get; set; } = "08:00";
        public string? BusinessHoursEnd { get; set; } = "18:00";

        [Display(Name = "Priority Condition")]
        public string? ConditionPriority { get; set; }

        [Display(Name = "Category Condition")]
        public string? ConditionCategory { get; set; }

        [Display(Name = "Notify Assignee on Breach")]
        public bool NotifyAssignee { get; set; } = true;
    }
}
