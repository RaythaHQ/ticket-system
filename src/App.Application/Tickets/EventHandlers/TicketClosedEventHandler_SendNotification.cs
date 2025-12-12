using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Events;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Sends email notification when a ticket is closed.
/// </summary>
public class TicketClosedEventHandler_SendNotification : INotificationHandler<TicketClosedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<TicketClosedEventHandler_SendNotification> _logger;

    public TicketClosedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        ICurrentOrganization currentOrganization,
        ILogger<TicketClosedEventHandler_SendNotification> logger)
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async ValueTask Handle(TicketClosedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = notification.Ticket;

            // Notify creator if they exist and are different from closer
            if (!ticket.CreatedByStaffId.HasValue)
                return;

            var creator = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.CreatedByStaffId.Value, cancellationToken);

            if (creator == null || string.IsNullOrEmpty(creator.EmailAddress))
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketClosedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var renderModel = new
            {
                TicketId = ticket.Id,
                TicketTitle = ticket.Title,
                ClosedByName = "System",
                ClosedAt = ticket.ClosedAt?.ToString("MMM dd, yyyy HH:mm") ?? DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm"),
                RecipientName = creator.FullName,
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
                To = new List<string> { creator.EmailAddress },
                Subject = subject
            };

            _emailerService.SendEmail(emailMessage);

            _logger.LogInformation(
                "Sent ticket closed notification for ticket {TicketId} to {Email}",
                ticket.Id,
                creator.EmailAddress
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket closed notification");
        }
    }
}

