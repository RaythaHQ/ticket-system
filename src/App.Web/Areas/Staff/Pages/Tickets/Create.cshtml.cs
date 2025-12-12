using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using App.Application.Tickets.Commands;
using App.Domain.ValueObjects;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.EntityFrameworkCore;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for creating a new ticket.
/// </summary>
public class Create : BaseStaffPageModel
{
    [BindProperty]
    public CreateTicketViewModel Form { get; set; } = new();

    public List<AssigneeSelectItem> AvailableAssignees { get; set; } = new();

    public async Task<IActionResult> OnGet(long? contactId, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Create Ticket";
        ViewData["ActiveMenu"] = "Tickets";

        // Pre-populate contact if provided
        if (contactId.HasValue)
        {
            Form.ContactId = contactId.Value;
        }

        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPost(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync(cancellationToken);
            return Page();
        }

        var command = new CreateTicket.Command
        {
            Title = Form.Title,
            Description = Form.Description,
            Priority = Form.Priority,
            Category = Form.Category,
            Tags = Form.Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
            OwningTeamId = Form.OwningTeamId,
            AssigneeId = Form.AssigneeId,
            ContactId = Form.ContactId
        };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage($"Ticket #{response.Result} created successfully.");
            return RedirectToPage(RouteNames.Tickets.Details, new { id = response.Result });
        }

        SetErrorMessage(response.GetErrors());
        await LoadSelectListsAsync(cancellationToken);
        return Page();
    }

    private async Task LoadSelectListsAsync(CancellationToken cancellationToken)
    {
        var canManageTickets = TicketPermissionService.CanManageTickets();
        var assignees = new List<AssigneeSelectItem>();

        // Get current user's team memberships
        var currentUserId = CurrentUser.UserId?.Guid;
        var userTeamIds = new HashSet<Guid>();
        if (currentUserId.HasValue)
        {
            userTeamIds = await Db.TeamMemberships
                .AsNoTracking()
                .Where(m => m.StaffAdminId == currentUserId.Value)
                .Select(m => m.TeamId)
                .ToHashSetAsync(cancellationToken);
        }

        // Add "Unassigned" option
        assignees.Add(new AssigneeSelectItem
        {
            Value = "unassigned",
            DisplayText = "Unassigned",
            TeamId = null,
            AssigneeId = null
        });

        // Load teams with their members
        var teams = await Db.Teams
            .AsNoTracking()
            .Include(t => t.Memberships)
                .ThenInclude(m => m.StaffAdmin)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        foreach (var team in teams)
        {
            // Add "Team Name/Anyone" option (team assigned, individual unassigned)
            var teamShortGuid = new ShortGuid(team.Id);
            assignees.Add(new AssigneeSelectItem
            {
                Value = $"team:{teamShortGuid}",
                DisplayText = $"{team.Name}/Anyone",
                TeamId = teamShortGuid,
                AssigneeId = null
            });

            // Add individual team members if:
            // 1. User has CanManageTickets permission (can assign to anyone in any team), OR
            // 2. User is a member of this team (can assign to others in their team)
            var showTeamMembers = canManageTickets || userTeamIds.Contains(team.Id);
            
            if (showTeamMembers)
            {
                var members = team.Memberships
                    .Where(m => m.StaffAdmin != null && m.StaffAdmin.IsActive)
                    .OrderBy(m => m.StaffAdmin!.FirstName)
                    .ThenBy(m => m.StaffAdmin!.LastName)
                    .ToList();

                foreach (var member in members)
                {
                    var memberShortGuid = new ShortGuid(member.StaffAdminId);
                    assignees.Add(new AssigneeSelectItem
                    {
                        Value = $"team:{teamShortGuid}:assignee:{memberShortGuid}",
                        DisplayText = $"{team.Name}/{member.StaffAdmin!.FirstName} {member.StaffAdmin!.LastName}",
                        TeamId = teamShortGuid,
                        AssigneeId = memberShortGuid
                    });
                }
            }
        }

        AvailableAssignees = assignees;
    }

    public record CreateTicketViewModel
    {
        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Priority { get; set; } = TicketPriority.NORMAL;

        public string? Category { get; set; }

        public string? Tags { get; set; }

        public ShortGuid? OwningTeamId { get; set; }

        public ShortGuid? AssigneeId { get; set; }

        public long? ContactId { get; set; }
    }

    public record AssigneeSelectItem
    {
        public string Value { get; init; } = string.Empty;
        public string DisplayText { get; init; } = string.Empty;
        public ShortGuid? TeamId { get; init; }
        public ShortGuid? AssigneeId { get; init; }
    }
}

