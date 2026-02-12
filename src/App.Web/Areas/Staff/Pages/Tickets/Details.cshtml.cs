using System.ComponentModel.DataAnnotations;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Application.Contacts;
using App.Application.Contacts.Queries;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Application.TicketConfig.Queries;
using App.Application.TicketTasks;
using App.Application.TicketTasks.Commands;
using App.Application.TicketTasks.Queries;
using App.Web.Areas.Staff.Pages.Shared;
using App.Web.Areas.Staff.Pages.Shared.Models;
using CSharpVitamins;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Staff.Pages.Tickets;

/// <summary>
/// Page model for viewing ticket details.
/// </summary>
public class Details : BaseStaffPageModel
{
    public TicketDto Ticket { get; set; } = null!;
    public ContactDto? Contact { get; set; }
    public IEnumerable<TicketCommentDto> Comments { get; set; } =
        Enumerable.Empty<TicketCommentDto>();
    public IEnumerable<TicketChangeLogEntryDto> ChangeLog { get; set; } =
        Enumerable.Empty<TicketChangeLogEntryDto>();

    [BindProperty]
    public AddCommentViewModel CommentForm { get; set; } = new();

    public bool CanEditTicket { get; set; }
    public bool CanDeleteTicket { get; set; }
    public bool CanManageTickets { get; set; }

    // Following feature
    public bool IsFollowing { get; set; }
    public IEnumerable<GetTicketFollowers.TicketFollowerDto> Followers { get; set; } =
        Enumerable.Empty<GetTicketFollowers.TicketFollowerDto>();

    // SLA Extension feature
    public SlaExtensionInfo SlaExtensionInfo { get; set; } = new();

    [BindProperty]
    public int ExtensionHours { get; set; }

    // Tasks feature
    public IEnumerable<TicketTaskDto> Tasks { get; set; } = Enumerable.Empty<TicketTaskDto>();
    public List<TaskTemplateListItemDto> TaskTemplates { get; set; } = new();
    public List<AssigneeSelectOptionDto> TaskAvailableAssignees { get; set; } = new();
    public bool IsTicketClosed { get; set; }

    // Snooze feature
    [BindProperty]
    public SnoozeViewModel SnoozeForm { get; set; } = new();

    /// <summary>
    /// Back to list URL - preserved across navigation and form submissions.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? BackToListUrl { get; set; }

    public async Task<IActionResult> OnGet(
        long id,
        string? backToListUrl = null,
        CancellationToken cancellationToken = default
    )
    {
        ViewData["Title"] = $"Ticket #{id}";
        ViewData["ActiveMenu"] = "Tickets";

        // NOTE: DbContext is scoped per-request and not thread-safe, so queries must be sequential
        var ticketResponse = await Mediator.Send(
            new GetTicketById.Query { Id = id },
            cancellationToken
        );
        Ticket = ticketResponse.Result;

        var commentsResponse = await Mediator.Send(
            new GetTicketComments.Query { TicketId = id },
            cancellationToken
        );
        Comments = commentsResponse.Result;

        var changeLogResponse = await Mediator.Send(
            new GetTicketChangeLog.Query { TicketId = id },
            cancellationToken
        );
        ChangeLog = changeLogResponse.Result;

        var isFollowingResponse = await Mediator.Send(
            new IsFollowingTicket.Query { TicketId = id },
            cancellationToken
        );
        IsFollowing = isFollowingResponse.Result;

        var followersResponse = await Mediator.Send(
            new GetTicketFollowers.Query { TicketId = id },
            cancellationToken
        );
        Followers = followersResponse.Result;

        var tasksResponse = await Mediator.Send(
            new GetTasksByTicketId.Query { TicketId = id },
            cancellationToken
        );
        Tasks = tasksResponse.Result;

        var templatesResponse = await Mediator.Send(
            new GetTaskTemplates.Query { ActiveOnly = true },
            cancellationToken
        );
        TaskTemplates = templatesResponse.Result.Items.ToList();

        var taskAssigneeResponse = await Mediator.Send(
            new GetAssigneeSelectOptions.Query
            {
                CanManageTickets = true,
                CurrentUserId = CurrentUser.UserId?.Guid,
            },
            cancellationToken
        );
        TaskAvailableAssignees = taskAssigneeResponse.Result.ToList();

        // Determine if the ticket is closed (by checking ClosedAt timestamp)
        IsTicketClosed = Ticket.ClosedAt.HasValue;

        // Load full contact details if contact is assigned (depends on ticket result)
        if (Ticket.ContactId.HasValue)
        {
            var contactResponse = await Mediator.Send(
                new GetContactById.Query { Id = Ticket.ContactId.Value },
                cancellationToken
            );
            Contact = contactResponse.Result;
        }

        // Check if user can edit this specific ticket (has permission, is assigned, or is in the team)
        CanEditTicket = await TicketPermissionService.CanEditTicketAsync(
            Ticket.AssigneeId?.Guid,
            Ticket.OwningTeamId?.Guid,
            cancellationToken
        );

        // Check if user can delete tickets (requires Can Manage Tickets permission)
        CanDeleteTicket = TicketPermissionService.CanManageTickets();
        CanManageTickets = TicketPermissionService.CanManageTickets();

        // Load SLA extension info
        SlaExtensionInfo = await GetSlaExtensionInfoAsync(Ticket, cancellationToken);

        // Store back URL for the view (use property if set, otherwise use parameter)
        BackToListUrl = backToListUrl ?? BackToListUrl;
        ViewData["BackToListUrl"] = BackToListUrl;

        return Page();
    }

    private async Task<SlaExtensionInfo> GetSlaExtensionInfoAsync(
        TicketDto ticket,
        CancellationToken cancellationToken
    )
    {
        var settings = SlaExtensionSettings.FromEnvironment();
        var hasUnlimited = TicketPermissionService.CanManageTickets();
        var canEdit = await TicketPermissionService.CanEditTicketAsync(
            ticket.AssigneeId?.Guid,
            ticket.OwningTeamId?.Guid,
            cancellationToken
        );

        var isClosed =
            ticket.Status == Domain.ValueObjects.TicketStatus.CLOSED
            || ticket.Status == Domain.ValueObjects.TicketStatus.RESOLVED;

        var atLimit = !hasUnlimited && ticket.SlaExtensionCount >= settings.MaxExtensions;

        string? cannotExtendReason = null;
        var canExtend = canEdit && !isClosed && !atLimit;

        if (!canEdit)
            cannotExtendReason = "You do not have permission to modify this ticket.";
        else if (isClosed)
            cannotExtendReason = "Cannot extend SLA on closed or resolved tickets.";
        else if (atLimit)
            cannotExtendReason = $"Maximum extensions ({settings.MaxExtensions}) reached.";

        // Get default extension hours from service
        var slaService = HttpContext.RequestServices.GetRequiredService<ISlaService>();
        var defaultHours = slaService.CalculateDefaultExtensionHours(
            ticket.SlaDueAt,
            CurrentOrganization.TimeZone
        );

        return new SlaExtensionInfo
        {
            CurrentSlaDueAt = ticket.SlaDueAt,
            ExtensionCount = ticket.SlaExtensionCount,
            MaxExtensions = settings.MaxExtensions,
            MaxExtensionHours = settings.MaxExtensionHours,
            HasUnlimitedExtensions = hasUnlimited,
            CanExtend = canExtend,
            CannotExtendReason = cannotExtendReason,
            DefaultExtensionHours = defaultHours,
            HasSlaRule = ticket.SlaRuleId.HasValue,
        };
    }

    public async Task<IActionResult> OnPostDelete(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteTicket.Command { Id = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Ticket deleted successfully.");
            // Return to the list URL if available, otherwise go to index
            if (!string.IsNullOrEmpty(BackToListUrl))
                return Redirect(BackToListUrl);
            return RedirectToPage(RouteNames.Tickets.Index);
        }
        else
        {
            SetErrorMessage(response.GetErrors());
            return await OnGet(id, BackToListUrl, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAddComment(long id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CommentForm.Body))
        {
            ModelState.AddModelError("CommentForm.Body", "Comment cannot be empty.");
            return await OnGet(id, BackToListUrl, cancellationToken);
        }

        var command = new AddTicketComment.Command { TicketId = id, Body = CommentForm.Body };

        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Comment added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostExtendSla(long id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new ExtendTicketSla.Command { Id = id, ExtensionHours = ExtensionHours };

            var response = await Mediator.Send(command, cancellationToken);

            if (response.Success)
            {
                SetSuccessMessage(
                    $"SLA extended by {ExtensionHours} hour{(ExtensionHours != 1 ? "s" : "")}."
                );
            }
            else
            {
                SetErrorMessage(response.GetErrors());
            }
        }
        catch (Application.Common.Exceptions.BusinessException ex)
        {
            SetErrorMessage(ex.Message);
        }
        catch (Application.Common.Exceptions.ForbiddenAccessException ex)
        {
            SetErrorMessage(ex.Message);
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnGetPreviewSlaExtension(
        long id,
        int hours,
        CancellationToken cancellationToken
    )
    {
        if (hours <= 0)
        {
            return new JsonResult(
                new
                {
                    dueDateUtc = (DateTime?)null,
                    dueDateFormatted = (string?)null,
                    valid = false,
                    error = "Hours must be greater than zero",
                }
            );
        }

        var ticketResponse = await Mediator.Send(
            new GetTicketById.Query { Id = id },
            cancellationToken
        );
        var ticket = ticketResponse.Result;

        var slaService = HttpContext.RequestServices.GetRequiredService<ISlaService>();
        var newDueDate = slaService.CalculateExtendedDueDate(ticket.SlaDueAt, hours);

        if (newDueDate <= DateTime.UtcNow)
        {
            return new JsonResult(
                new
                {
                    dueDateUtc = (DateTime?)null,
                    dueDateFormatted = (string?)null,
                    valid = false,
                    error = "Extension would result in a due date in the past",
                }
            );
        }

        var formatted = CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
            newDueDate
        );

        return new JsonResult(
            new
            {
                dueDateUtc = newDueDate,
                dueDateFormatted = formatted,
                valid = true,
                error = (string?)null,
            }
        );
    }

    public async Task<IActionResult> OnPostRefreshSla(
        long id,
        bool restartFromNow,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new RefreshTicketSla.Command { Id = id, RestartFromNow = restartFromNow };

            var response = await Mediator.Send(command, cancellationToken);

            if (response.Success)
            {
                var message = restartFromNow
                    ? "SLA restarted from current time."
                    : "SLA rules re-evaluated.";
                SetSuccessMessage(message);
            }
            else
            {
                SetErrorMessage(response.GetErrors());
            }
        }
        catch (Application.Common.Exceptions.BusinessException ex)
        {
            SetErrorMessage(ex.Message);
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostFollow(long id, CancellationToken cancellationToken)
    {
        var command = new FollowTicket.Command { TicketId = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("You are now following this ticket.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostUnfollow(long id, CancellationToken cancellationToken)
    {
        var command = new UnfollowTicket.Command { TicketId = id };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("You are no longer following this ticket.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostAddFollower(
        long id,
        ShortGuid staffAdminId,
        CancellationToken cancellationToken
    )
    {
        var command = new AddTicketFollower.Command { TicketId = id, StaffAdminId = staffAdminId };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Follower added successfully.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostRemoveFollower(
        long id,
        ShortGuid staffAdminId,
        CancellationToken cancellationToken
    )
    {
        var command = new RemoveTicketFollower.Command
        {
            TicketId = id,
            StaffAdminId = staffAdminId,
        };
        var response = await Mediator.Send(command, cancellationToken);

        if (response.Success)
        {
            SetSuccessMessage("Follower removed.");
        }
        else
        {
            SetErrorMessage(response.GetErrors());
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    /// <summary>
    /// API endpoint for searching mentionable teams and users for @ autocomplete.
    /// </summary>
    public async Task<IActionResult> OnGetSearchMentionables(
        string? query,
        CancellationToken cancellationToken
    )
    {
        var response = await Mediator.Send(
            new SearchMentionables.Query { SearchTerm = query ?? string.Empty, MaxResults = 10 },
            cancellationToken
        );

        var result = response.Result;
        return new JsonResult(
            new
            {
                teams = result.Teams.Select(t => new
                {
                    id = t.Id.ToString(),
                    name = t.Name,
                    memberCount = t.MemberCount,
                    type = "team",
                }),
                users = result.Users.Select(u => new
                {
                    id = u.Id.ToString(),
                    name = u.Name,
                    email = u.Email,
                    type = "user",
                }),
            }
        );
    }

    public record AddCommentViewModel
    {
        [Required]
        public string Body { get; set; } = string.Empty;
    }

    public record SnoozeViewModel
    {
        public DateTime? SnoozeUntil { get; set; }
        public string? Reason { get; set; }
    }

    public async Task<IActionResult> OnPostSnooze(long id, CancellationToken cancellationToken)
    {
        try
        {
            if (!SnoozeForm.SnoozeUntil.HasValue)
            {
                SetErrorMessage("Please select a date and time to snooze until.");
                return RedirectToPage(
                    RouteNames.Tickets.Details,
                    new { id, backToListUrl = BackToListUrl }
                );
            }

            // Convert from organization timezone to UTC for storage/validation
            var snoozeUntilUtc = SnoozeForm.SnoozeUntil.Value.TimeZoneToUtc(CurrentOrganization.TimeZone);
            
            var command = new SnoozeTicket.Command
            {
                TicketId = id,
                SnoozeUntil = snoozeUntilUtc,
                Reason = SnoozeForm.Reason,
            };

            var response = await Mediator.Send(command, cancellationToken);

            if (response.Success)
            {
                SetSuccessMessage(
                    $"Ticket snoozed until {CurrentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(response.Result.SnoozedUntil)}."
                );
            }
            else
            {
                SetErrorMessage(response.GetErrors());
            }
        }
        catch (Application.Common.Exceptions.BusinessException ex)
        {
            SetErrorMessage(ex.Message);
        }
        catch (Application.Common.Exceptions.ForbiddenAccessException ex)
        {
            SetErrorMessage(ex.Message);
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    public async Task<IActionResult> OnPostUnsnooze(long id, CancellationToken cancellationToken)
    {
        try
        {
            var command = new UnsnoozeTicket.Command { TicketId = id };

            var response = await Mediator.Send(command, cancellationToken);

            if (response.Success)
            {
                SetSuccessMessage("Ticket unsnoozed.");
            }
            else
            {
                SetErrorMessage(response.GetErrors());
            }
        }
        catch (Application.Common.Exceptions.BusinessException ex)
        {
            SetErrorMessage(ex.Message);
        }
        catch (Application.Common.Exceptions.NotFoundException)
        {
            SetErrorMessage("Ticket not found or not snoozed.");
        }

        return RedirectToPage(RouteNames.Tickets.Details, new { id, backToListUrl = BackToListUrl });
    }

    // ──── Task Handlers ────

    public async Task<IActionResult> OnPostCreateTask(long id, [FromBody] CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTicketTask.Command { TicketId = id, Title = request.Title };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostUpdateTask(long id, [FromBody] UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTicketTask.Command
        {
            TaskId = request.TaskId,
            Title = request.Title,
            AssigneeId = request.AssigneeId,
            OwningTeamId = request.OwningTeamId,
            DueAt = request.DueAt,
            DependsOnTaskId = request.DependsOnTaskId,
            ClearAssignee = request.ClearAssignee,
            ClearDueAt = request.ClearDueAt,
            ClearDependency = request.ClearDependency,
        };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostCompleteTask(long id, [FromBody] TaskIdRequest request, CancellationToken cancellationToken)
    {
        var command = new CompleteTicketTask.Command { TaskId = request.TaskId };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostReopenTask(long id, [FromBody] TaskIdRequest request, CancellationToken cancellationToken)
    {
        var command = new ReopenTicketTask.Command { TaskId = request.TaskId };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostDeleteTask(long id, [FromBody] TaskIdRequest request, CancellationToken cancellationToken)
    {
        var command = new DeleteTicketTask.Command { TaskId = request.TaskId };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, result = response.Result, error = response.Error });
    }

    public async Task<IActionResult> OnPostReorderTasks(long id, [FromBody] ReorderTasksRequest request, CancellationToken cancellationToken)
    {
        var command = new ReorderTicketTasks.Command { TicketId = id, OrderedIds = request.OrderedIds };
        var response = await Mediator.Send(command, cancellationToken);
        return new JsonResult(new { success = response.Success, error = response.Error });
    }

    public async Task<IActionResult> OnPostApplyTaskTemplate(long id, [FromBody] ApplyTemplateRequest request, CancellationToken cancellationToken)
    {
        var command = new ApplyTaskTemplate.Command
        {
            TicketId = id,
            TemplateId = request.TemplateId,
        };
        var response = await Mediator.Send(command, cancellationToken);
        if (response.Success)
        {
            return new JsonResult(new { success = true, tasks = response.Result });
        }
        return new JsonResult(new { success = false, error = response.Error });
    }

    // ──── Task Request Models ────

    public record CreateTaskRequest { public string Title { get; init; } = string.Empty; }
    public record UpdateTaskRequest
    {
        public ShortGuid TaskId { get; init; }
        public string? Title { get; init; }
        public ShortGuid? AssigneeId { get; init; }
        public ShortGuid? OwningTeamId { get; init; }
        public DateTime? DueAt { get; init; }
        public ShortGuid? DependsOnTaskId { get; init; }
        public bool ClearAssignee { get; init; }
        public bool ClearDueAt { get; init; }
        public bool ClearDependency { get; init; }
    }
    public record TaskIdRequest { public ShortGuid TaskId { get; init; } }
    public record ReorderTasksRequest { public List<ShortGuid> OrderedIds { get; init; } = new(); }
    public record ApplyTemplateRequest { public ShortGuid TemplateId { get; init; } }
}

/// <summary>
/// View model for the tasks section partial.
/// </summary>
public class TasksSectionModel
{
    public IEnumerable<TicketTaskDto> Tasks { get; set; } = Enumerable.Empty<TicketTaskDto>();
    public List<TaskTemplateListItemDto> Templates { get; set; } = new();
    public List<AssigneeSelectOptionDto> AvailableAssignees { get; set; } = new();
    public bool IsTicketClosed { get; set; }
}

/// <summary>
/// View model for SLA extension state and capabilities.
/// </summary>
public class SlaExtensionInfo
{
    public DateTime? CurrentSlaDueAt { get; init; }
    public int ExtensionCount { get; init; }
    public int MaxExtensions { get; init; }
    public int MaxExtensionHours { get; init; }
    public bool HasUnlimitedExtensions { get; init; }
    public bool CanExtend { get; init; }
    public string? CannotExtendReason { get; init; }
    public int DefaultExtensionHours { get; init; }
    public bool HasSlaRule { get; init; }
}
