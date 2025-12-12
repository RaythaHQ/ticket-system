using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Interfaces;
using App.Application.SlaRules;
using App.Application.SlaRules.Commands;
using App.Application.SlaRules.Queries;
using App.Web.Areas.Admin.Pages.Shared;
using App.Web.Areas.Admin.Pages.Shared.Models;

namespace App.Web.Areas.Admin.Pages.SlaRules;

/// <summary>
/// Page model for editing an SLA rule.
/// </summary>
public class Edit : BaseAdminPageModel
{
    private readonly ITicketPermissionService _permissionService;

    public Edit(ITicketPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public SlaRuleDto Rule { get; set; } = null!;

    [BindProperty]
    public EditSlaRuleViewModel Form { get; set; } = new();

    public async Task<IActionResult> OnGet(string id, CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTickets())
            return Forbid();

        var response = await Mediator.Send(new GetSlaRuleById.Query { Id = id }, cancellationToken);
        Rule = response.Result;

        var (value, unit) = ParseMinutes(Rule.TargetResolutionMinutes);

        Form = new EditSlaRuleViewModel
        {
            Id = Rule.Id.ToString(),
            Name = Rule.Name,
            Description = Rule.Description,
            TargetResolutionValue = value,
            TargetResolutionUnit = unit,
            Priority = Rule.Priority,
            BusinessHoursEnabled = Rule.BusinessHoursEnabled,
            BusinessHoursStart = Rule.BusinessHoursConfig?.StartTime ?? "08:00",
            BusinessHoursEnd = Rule.BusinessHoursConfig?.EndTime ?? "18:00",
            ConditionPriority = Rule.Conditions.ContainsKey("priority") ? Rule.Conditions["priority"]?.ToString() : null,
            ConditionCategory = Rule.Conditions.ContainsKey("category") ? Rule.Conditions["category"]?.ToString() : null,
            NotifyAssignee = Rule.BreachBehavior?.NotifyAssignee ?? true,
            IsActive = Rule.IsActive
        };

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
                Workdays = new List<int> { 1, 2, 3, 4, 5 },
                StartTime = Form.BusinessHoursStart ?? "08:00",
                EndTime = Form.BusinessHoursEnd ?? "18:00"
            };
        }

        var command = new UpdateSlaRule.Command
        {
            Id = Form.Id,
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
                NotifyAssignee = Form.NotifyAssignee
            },
            IsActive = Form.IsActive
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("SLA rule updated successfully.");
            return RedirectToPage(RouteNames.SlaRules.Index);
        }

        SetErrorMessage(response.GetErrors());
        return Page();
    }

    public async Task<IActionResult> OnPostDelete(string id, CancellationToken cancellationToken)
    {
        if (!_permissionService.CanManageTickets())
            return Forbid();

        var command = new DeleteSlaRule.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("SLA rule deleted successfully.");
            return RedirectToPage(RouteNames.SlaRules.Index);
        }

        SetErrorMessage(response.GetErrors());
        return await OnGet(id, cancellationToken);
    }

    private int CalculateMinutes(int value, string unit)
    {
        return unit switch
        {
            "minutes" => value,
            "hours" => value * 60,
            "days" => value * 1440,
            _ => value * 60
        };
    }

    private (int value, string unit) ParseMinutes(int minutes)
    {
        if (minutes % 1440 == 0)
            return (minutes / 1440, "days");
        if (minutes % 60 == 0)
            return (minutes / 60, "hours");
        return (minutes, "minutes");
    }

    public record EditSlaRuleViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 10000)]
        public int TargetResolutionValue { get; set; } = 4;

        public string TargetResolutionUnit { get; set; } = "hours";

        [Range(0, 1000)]
        public int Priority { get; set; }

        public bool BusinessHoursEnabled { get; set; }
        public string? BusinessHoursStart { get; set; }
        public string? BusinessHoursEnd { get; set; }

        public string? ConditionPriority { get; set; }
        public string? ConditionCategory { get; set; }

        public bool NotifyAssignee { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}

