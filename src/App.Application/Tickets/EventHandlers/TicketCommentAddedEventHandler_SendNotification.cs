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
/// Sends email notification when a comment is added to a ticket.
/// Notifies the ticket assignee and creator if different from the commenter.
/// </summary>
public class TicketCommentAddedEventHandler_SendNotification : INotificationHandler<TicketCommentAddedEvent>
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailerService;
    private readonly IRenderEngine _renderEngineService;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<TicketCommentAddedEventHandler_SendNotification> _logger;

    public TicketCommentAddedEventHandler_SendNotification(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        ICurrentOrganization currentOrganization,
        ILogger<TicketCommentAddedEventHandler_SendNotification> logger)
    {
        _db = db;
        _emailerService = emailerService;
        _renderEngineService = renderEngineService;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async ValueTask Handle(TicketCommentAddedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = notification.Ticket;
            var comment = notification.Comment;

            // Get users to notify (assignee and creator, excluding commenter)
            var usersToNotify = new HashSet<Guid>();

            if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != comment.AuthorStaffId)
                usersToNotify.Add(ticket.AssigneeId.Value);

            if (ticket.CreatedByStaffId.HasValue && ticket.CreatedByStaffId.Value != comment.AuthorStaffId)
                usersToNotify.Add(ticket.CreatedByStaffId.Value);

            if (!usersToNotify.Any())
                return;

            var renderTemplate = _db.EmailTemplates.FirstOrDefault(p =>
                p.DeveloperName == BuiltInEmailTemplate.TicketCommentAddedEmail.DeveloperName
            );

            if (renderTemplate == null)
                return;

            var commenter = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == comment.AuthorStaffId, cancellationToken);

            var users = await _db.Users
                .AsNoTracking()
                .Where(u => usersToNotify.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var user in users.Where(u => !string.IsNullOrEmpty(u.EmailAddress)))
            {
                var renderModel = new TicketCommentAdded_RenderModel
                {
                    TicketId = ticket.Id,
                    Title = ticket.Title,
                    CommentAuthor = commenter?.FullName ?? "Unknown",
                    CommentBody = comment.Body,
                    RecipientName = user.FullName,
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
                    To = new List<string> { user.EmailAddress },
                    Subject = subject
                };

                _emailerService.SendEmail(emailMessage);

                _logger.LogInformation(
                    "Sent comment notification for ticket {TicketId} to {Email}",
                    ticket.Id,
                    user.EmailAddress
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ticket comment notification");
        }
    }
}

