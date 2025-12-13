using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.Tickets.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email notification when an SLA is approaching breach.
/// </summary>
public class SlaApproachingEventHandler_SendNotification : INotificationHandler<SlaApproachingBreachEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<SlaApproachingEventHandler_SendNotification> _logger;

    public SlaApproachingEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        ICurrentOrganization currentOrganization,
        ILogger<SlaApproachingEventHandler_SendNotification> logger)
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async ValueTask Handle(SlaApproachingBreachEvent notification, CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;

        if (!ticket.AssigneeId.HasValue)
            return;

        try
        {
            var assignee = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);

            if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.SlaApproachingEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var slaRule = ticket.SlaRuleId.HasValue
                ? await _db.SlaRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ticket.SlaRuleId.Value, cancellationToken)
                : null;

            var timeRemaining = ticket.SlaDueAt.HasValue
                ? FormatTimeRemaining(ticket.SlaDueAt.Value - DateTime.UtcNow)
                : "Unknown";

            var renderModel = new SlaApproaching_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                AssigneeName = assignee.FullName,
                Priority = ticket.Priority,
                SlaDueAt = ticket.SlaDueAt?.ToString("MMM dd, yyyy h:mm tt") ?? "-",
                TimeRemaining = timeRemaining,
                SlaRuleName = slaRule?.Name ?? "Unknown",
                TicketUrl = $"{_currentOrganization.PathBase}/staff/tickets/{ticket.Id}"
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(_currentOrganization),
                Target = renderModel
            };

            var subject = _renderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
            var content = _renderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { assignee.EmailAddress },
                Subject = subject
            };

            _emailerService.SendEmail(emailMessage);

            _logger.LogInformation(
                "Sent SLA approaching notification for ticket {TicketId} to {Email}",
                ticket.Id,
                assignee.EmailAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SLA approaching notification");
        }
    }

    private static string FormatTimeRemaining(TimeSpan remaining)
    {
        if (remaining.TotalDays >= 1)
            return $"{(int)remaining.TotalDays} day{((int)remaining.TotalDays > 1 ? "s" : "")}";
        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours} hour{((int)remaining.TotalHours > 1 ? "s" : "")}";
        if (remaining.TotalMinutes >= 1)
            return $"{(int)remaining.TotalMinutes} minute{((int)remaining.TotalMinutes > 1 ? "s" : "")}";
        return "Less than a minute";
    }
}
