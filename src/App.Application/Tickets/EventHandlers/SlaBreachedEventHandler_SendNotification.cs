using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.SlaRules;
using App.Application.Tickets.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email notification when an SLA is breached.
/// </summary>
public class SlaBreachedEventHandler_SendNotification : INotificationHandler<SlaBreachedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<SlaBreachedEventHandler_SendNotification> _logger;

    public SlaBreachedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<SlaBreachedEventHandler_SendNotification> logger
    )
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilder = relativeUrlBuilder;
        _currentOrganization = currentOrganization;
        _notificationPreferenceService = notificationPreferenceService;
        _logger = logger;
    }

    public async ValueTask Handle(
        SlaBreachedEvent notification,
        CancellationToken cancellationToken
    )
    {
        var ticket = notification.Ticket;

        try
        {
            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.SlaBreachedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var slaRule = ticket.SlaRuleId.HasValue
                ? await _db
                    .SlaRules.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == ticket.SlaRuleId.Value, cancellationToken)
                : null;

            // Parse breach behavior to get notification settings
            BreachBehavior? breachBehavior = null;
            if (!string.IsNullOrEmpty(slaRule?.BreachBehaviorJson))
            {
                try
                {
                    breachBehavior = JsonSerializer.Deserialize<BreachBehavior>(
                        slaRule.BreachBehaviorJson
                    );
                }
                catch
                {
                    // Ignore deserialization errors
                }
            }

            // Get assignee info for the render model
            User? assignee = null;
            if (ticket.AssigneeId.HasValue)
            {
                assignee = await _db
                    .Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);
            }

            var renderModel = new SlaBreach_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                AssigneeName = assignee?.FullName ?? "Unassigned",
                Priority = ticket.Priority,
                SlaDueAt = _currentOrganization.TimeZoneConverter.UtcToTimeZoneAsDateTimeFormat(
                    ticket.SlaDueAt
                ),
                SlaRuleName = slaRule?.Name ?? "Unknown",
                TicketUrl = _relativeUrlBuilder.StaffTicketUrl(ticket.Id),
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                    _currentOrganization
                ),
                Target = renderModel,
            };

            var subject = _renderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
            var content = _renderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

            // Send to assignee if enabled
            if (
                ticket.AssigneeId.HasValue
                && assignee != null
                && !string.IsNullOrEmpty(assignee.EmailAddress)
                && (breachBehavior?.NotifyAssignee ?? true)
            )
            {
                var emailEnabled = await _notificationPreferenceService.IsEmailEnabledAsync(
                    ticket.AssigneeId.Value,
                    NotificationEventType.SLA_BREACHED,
                    cancellationToken
                );

                if (emailEnabled)
                {
                    var emailMessage = new EmailMessage
                    {
                        Content = content,
                        To = new List<string> { assignee.EmailAddress },
                        Subject = subject,
                    };

                    _emailerService.SendEmail(emailMessage);

                    _logger.LogInformation(
                        "Sent SLA breach notification for ticket {TicketId} to assignee {Email}",
                        ticket.Id,
                        assignee.EmailAddress
                    );
                }
            }

            // Send to additional notification emails
            if (!string.IsNullOrWhiteSpace(breachBehavior?.AdditionalNotificationEmails))
            {
                var additionalEmails = breachBehavior
                    .AdditionalNotificationEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();

                foreach (var email in additionalEmails)
                {
                    var emailMessage = new EmailMessage
                    {
                        Content = content,
                        To = new List<string> { email },
                        Subject = subject,
                    };

                    _emailerService.SendEmail(emailMessage);

                    _logger.LogInformation(
                        "Sent SLA breach notification for ticket {TicketId} to additional recipient {Email}",
                        ticket.Id,
                        email
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SLA breach notification");
        }
    }
}
