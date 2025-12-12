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
/// Sends email notification when a ticket is assigned.
/// </summary>
public class TicketAssignedEventHandler_SendNotification : INotificationHandler<TicketAssignedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly IRelativeUrlBuilder _relativeUrlBuilderService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<TicketAssignedEventHandler_SendNotification> _logger;

    public TicketAssignedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilderService,
        ICurrentOrganization currentOrganization,
        ILogger<TicketAssignedEventHandler_SendNotification> logger)
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _relativeUrlBuilderService = relativeUrlBuilderService;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async ValueTask Handle(TicketAssignedEvent notification, CancellationToken cancellationToken)
    {
        // Only notify if assignee changed and new assignee exists
        if (!notification.NewAssigneeId.HasValue || notification.OldAssigneeId == notification.NewAssigneeId)
            return;

        try
        {
            var ticket = notification.Ticket;
            var assignee = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == notification.NewAssigneeId.Value, cancellationToken);

            if (assignee == null || string.IsNullOrEmpty(assignee.EmailAddress))
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketAssignedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var renderModel = new TicketAssigned_RenderModel
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                Priority = ticket.Priority,
                Status = ticket.Status,
                Category = ticket.Category,
                AssigneeName = assignee.FullName,
                ContactName = ticket.Contact?.Name,
                TeamName = ticket.OwningTeam?.Name,
                SlaDueAt = ticket.SlaDueAt?.ToString("MMM dd, yyyy HH:mm"),
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
                "Sent ticket assignment notification for ticket {TicketId} to {Email}",
                ticket.Id,
                assignee.EmailAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket assignment notification");
        }
    }
}
