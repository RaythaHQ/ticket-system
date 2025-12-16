using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email notification when a ticket is reopened.
/// </summary>
public class TicketReopenedEventHandler_SendNotification : INotificationHandler<TicketReopenedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilder;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly INotificationPreferenceService _notificationPreferenceService;
    private readonly ILogger<TicketReopenedEventHandler_SendNotification> _logger;

    public TicketReopenedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        ILogger<TicketReopenedEventHandler_SendNotification> logger
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
        TicketReopenedEvent notification,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var ticket = notification.Ticket;

            // Notify assignee if they exist
            if (!ticket.AssigneeId.HasValue)
                return;

            // Don't notify if the assignee is the one who reopened the ticket
            if (
                notification.ReopenedByUserId.HasValue
                && ticket.AssigneeId.Value == notification.ReopenedByUserId.Value
            )
                return;

            // Check notification preferences
            var emailEnabled = await _notificationPreferenceService.IsEmailEnabledAsync(
                ticket.AssigneeId.Value,
                NotificationEventType.TICKET_REOPENED,
                cancellationToken
            );

            if (!emailEnabled)
                return;

            var assignee = await _db
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);

            if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketReopenedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var renderModel = new
            {
                TicketId = ticket.Id,
                TicketTitle = ticket.Title,
                ReopenedByName = "System",
                Status = ticket.Status,
                RecipientName = assignee.FullName,
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

            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { assignee.EmailAddress },
                Subject = subject,
            };

            _emailerService.SendEmail(emailMessage);

            _logger.LogInformation(
                "Sent ticket reopened notification for ticket {TicketId} to {Email}",
                ticket.Id,
                assignee.EmailAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket reopened notification");
        }
    }
}
