using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Domain.Common;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace App.Application.Tickets.EventHandlers;

/// <summary>
/// Base class for ticket notification event handlers.
/// Provides shared dependencies, suppression checks, and email rendering/sending helpers.
/// </summary>
public abstract class BaseTicketNotificationHandler
{
    protected readonly IAppDbContext Db;
    protected readonly IEmailer EmailerService;
    protected readonly IRenderEngine RenderEngineService;
    protected readonly IRelativeUrlBuilder RelativeUrlBuilder;
    protected readonly ICurrentOrganization CurrentOrganization;
    protected readonly INotificationPreferenceService NotificationPreferenceService;
    protected readonly IInAppNotificationService InAppNotificationService;
    protected readonly INotificationSuppressionService NotificationSuppressionService;
    protected readonly ILogger Logger;

    protected BaseTicketNotificationHandler(
        IAppDbContext db,
        IEmailer emailerService,
        IRenderEngine renderEngineService,
        IRelativeUrlBuilder relativeUrlBuilder,
        ICurrentOrganization currentOrganization,
        INotificationPreferenceService notificationPreferenceService,
        IInAppNotificationService inAppNotificationService,
        INotificationSuppressionService notificationSuppressionService,
        ILogger logger
    )
    {
        Db = db;
        EmailerService = emailerService;
        RenderEngineService = renderEngineService;
        RelativeUrlBuilder = relativeUrlBuilder;
        CurrentOrganization = currentOrganization;
        NotificationPreferenceService = notificationPreferenceService;
        InAppNotificationService = inAppNotificationService;
        NotificationSuppressionService = notificationSuppressionService;
        Logger = logger;
    }

    /// <summary>
    /// Returns true if notifications are currently suppressed (e.g., during imports).
    /// Logs a debug message when suppressed.
    /// </summary>
    protected bool ShouldSuppressNotifications(long ticketId, string eventDescription)
    {
        if (NotificationSuppressionService.ShouldSuppressNotifications())
        {
            Logger.LogDebug(
                "Notifications suppressed for {EventDescription} on ticket {TicketId}",
                eventDescription,
                ticketId
            );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Wraps a render model with the current organization context for Liquid template rendering.
    /// </summary>
    protected Wrapper_RenderModel WrapRenderModel(object target)
    {
        return new Wrapper_RenderModel
        {
            CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                CurrentOrganization
            ),
            Target = target,
        };
    }

    /// <summary>
    /// Looks up an email template, renders the subject and content with the given model,
    /// and sends the email to a single recipient. Returns false if the template was not found.
    /// </summary>
    protected async Task<bool> RenderAndSendEmailAsync(
        string templateDeveloperName,
        string recipientEmail,
        object renderModel,
        CancellationToken cancellationToken
    )
    {
        var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
            p.DeveloperName == templateDeveloperName
        );

        if (renderTemplate == null)
            return false;

        var wrappedModel = WrapRenderModel(renderModel);

        var subject = RenderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
        var content = RenderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

        var emailMessage = new EmailMessage
        {
            Content = content,
            To = new List<string> { recipientEmail },
            Subject = subject,
        };

        await EmailerService.SendEmailAsync(emailMessage, cancellationToken);
        return true;
    }

    /// <summary>
    /// Renders an email template and sends to multiple recipients, each with a per-recipient render model.
    /// Returns false if the template was not found.
    /// </summary>
    protected async Task<bool> RenderAndSendEmailsAsync(
        string templateDeveloperName,
        IEnumerable<User> recipients,
        Func<User, object> buildRenderModel,
        CancellationToken cancellationToken
    )
    {
        var renderTemplate = Db.EmailTemplates.FirstOrDefault(p =>
            p.DeveloperName == templateDeveloperName
        );

        if (renderTemplate == null)
            return false;

        foreach (var recipient in recipients)
        {
            if (string.IsNullOrEmpty(recipient.EmailAddress))
                continue;

            var renderModel = buildRenderModel(recipient);
            var wrappedModel = WrapRenderModel(renderModel);

            var subject = RenderEngineService.RenderAsHtml(renderTemplate.Subject, wrappedModel);
            var content = RenderEngineService.RenderAsHtml(renderTemplate.Content, wrappedModel);

            var emailMessage = new EmailMessage
            {
                Content = content,
                To = new List<string> { recipient.EmailAddress },
                Subject = subject,
            };

            await EmailerService.SendEmailAsync(emailMessage, cancellationToken);
        }

        return true;
    }
}
