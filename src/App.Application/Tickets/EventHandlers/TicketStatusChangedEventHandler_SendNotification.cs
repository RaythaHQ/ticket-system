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
/// Sends email notification when a ticket's status changes.
/// </summary>
public class TicketStatusChangedEventHandler_SendNotification : INotificationHandler<TicketStatusChangedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<TicketStatusChangedEventHandler_SendNotification> _logger;

    public TicketStatusChangedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        ICurrentOrganization currentOrganization,
        ILogger<TicketStatusChangedEventHandler_SendNotification> logger)
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async ValueTask Handle(TicketStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = notification.Ticket;

            // Notify assignee if they exist
            if (!ticket.AssigneeId.HasValue)
                return;

            var assignee = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.AssigneeId.Value, cancellationToken);

            if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketStatusChangedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var renderModel = new TicketStatusChanged_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                OldStatus = notification.OldStatus,
                NewStatus = notification.NewStatus,
                ChangedBy = "System", // Could be enhanced to track who made the change
                RecipientName = assignee.FullName,
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
                "Sent status change notification for ticket {TicketId} to {Email}",
                ticket.Id,
                assignee.EmailAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket status change notification");
        }
    }
}

