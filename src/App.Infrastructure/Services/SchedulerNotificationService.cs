using App.Application.Common.Interfaces;
using App.Application.Common.Models.RenderModels;
using App.Application.Scheduler.RenderModels;
using App.Application.Scheduler.Services;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Infrastructure.Services;

/// <summary>
/// Sends scheduler email notifications (confirmation, reminder, post-meeting) to both
/// the contact and assigned staff member using Liquid-rendered SchedulerEmailTemplates.
/// </summary>
public class SchedulerNotificationService : ISchedulerNotificationService
{
    private readonly IAppDbContext _db;
    private readonly IEmailer _emailer;
    private readonly IRenderEngine _renderEngine;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ILogger<SchedulerNotificationService> _logger;

    public SchedulerNotificationService(
        IAppDbContext db,
        IEmailer emailer,
        IRenderEngine renderEngine,
        ICurrentOrganization currentOrganization,
        ILogger<SchedulerNotificationService> logger
    )
    {
        _db = db;
        _emailer = emailer;
        _renderEngine = renderEngine;
        _currentOrganization = currentOrganization;
        _logger = logger;
    }

    public async Task SendConfirmationAsync(
        long appointmentId,
        CancellationToken cancellationToken = default
    )
    {
        await SendNotificationAsync(
            appointmentId,
            SchedulerEmailTemplate.TYPE_CONFIRMATION,
            cancellationToken
        );
    }

    public async Task SendReminderAsync(
        long appointmentId,
        CancellationToken cancellationToken = default
    )
    {
        await SendNotificationAsync(
            appointmentId,
            SchedulerEmailTemplate.TYPE_REMINDER,
            cancellationToken
        );
    }

    public async Task SendPostMeetingAsync(
        long appointmentId,
        CancellationToken cancellationToken = default
    )
    {
        await SendNotificationAsync(
            appointmentId,
            SchedulerEmailTemplate.TYPE_POST_MEETING,
            cancellationToken
        );
    }

    private async Task SendNotificationAsync(
        long appointmentId,
        string templateType,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Load appointment with related entities
            var appointment = await _db
                .Appointments.Include(a => a.Contact)
                .Include(a => a.AssignedStaffMember)
                    .ThenInclude(s => s.User)
                .Include(a => a.AppointmentType)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

            if (appointment == null)
            {
                _logger.LogWarning(
                    "Cannot send {TemplateType} notification: appointment {AppointmentId} not found",
                    templateType,
                    appointmentId
                );
                return;
            }

            // Load the scheduler email template by type and email channel
            var template = await _db
                .SchedulerEmailTemplates.AsNoTracking()
                .FirstOrDefaultAsync(
                    t =>
                        t.TemplateType == templateType
                        && t.Channel == SchedulerEmailTemplate.CHANNEL_EMAIL
                        && t.IsActive,
                    cancellationToken
                );

            if (template == null)
            {
                _logger.LogWarning(
                    "Scheduler email template not found for type {TemplateType} and channel email",
                    templateType
                );
                return;
            }

            // Build render model with org timezone conversion
            var tzConverter = _currentOrganization.TimeZoneConverter;
            var localDateTime = tzConverter.UtcToTimeZone(appointment.ScheduledStartTime);

            var renderModel = new AppointmentNotification_RenderModel
            {
                AppointmentCode = appointment.Code,
                MeetingLink = appointment.MeetingLink ?? string.Empty,
                AppointmentType = appointment.AppointmentType?.Name ?? string.Empty,
                AppointmentMode = AppointmentMode.From(appointment.Mode).Label,
                DateTime = localDateTime.ToString(tzConverter.DateTimeFormat),
                Duration = $"{appointment.DurationMinutes} minutes",
                StaffName = appointment.AssignedStaffMember?.User != null
                    ? $"{appointment.AssignedStaffMember.User.FirstName} {appointment.AssignedStaffMember.User.LastName}"
                    : string.Empty,
                StaffEmail =
                    appointment.AssignedStaffMember?.User?.EmailAddress ?? string.Empty,
                ContactName = appointment.Contact?.FullName ?? string.Empty,
                ContactEmail = appointment.Contact?.Email ?? string.Empty,
                ContactZipcode = appointment.Contact?.Zipcode ?? string.Empty,
                AppointmentContactFirstName = appointment.ContactFirstName ?? string.Empty,
                AppointmentContactLastName = appointment.ContactLastName ?? string.Empty,
                AppointmentContactEmail = appointment.ContactEmail ?? string.Empty,
                AppointmentContactPhone = appointment.ContactPhone ?? string.Empty,
                AppointmentContactAddress = appointment.ContactAddress ?? string.Empty,
                Notes = appointment.Notes ?? string.Empty,
            };

            var wrappedModel = new Wrapper_RenderModel
            {
                CurrentOrganization = CurrentOrganization_RenderModel.GetProjection(
                    _currentOrganization
                ),
                Target = renderModel,
            };

            // Render subject and content
            var subject = _renderEngine.RenderAsHtml(template.Subject ?? string.Empty, wrappedModel);
            var content = _renderEngine.RenderAsHtml(template.Content, wrappedModel);

            // Send to contact email
            var contactEmail = appointment.Contact?.Email;
            if (string.IsNullOrWhiteSpace(contactEmail))
            {
                _logger.LogWarning(
                    "Contact {ContactId} has no email address; skipping {TemplateType} notification for appointment {AppointmentId}",
                    appointment.ContactId,
                    templateType,
                    appointmentId
                );
            }
            else
            {
                var contactMessage = new EmailMessage
                {
                    Content = content,
                    To = new List<string> { contactEmail },
                    Subject = subject,
                };

                await _emailer.SendEmailAsync(contactMessage, cancellationToken);

                _logger.LogInformation(
                    "Sent {TemplateType} email to contact {ContactEmail} for appointment {AppointmentCode}",
                    templateType,
                    contactEmail,
                    appointment.Code
                );
            }

            // Send to staff email
            var staffEmail = appointment.AssignedStaffMember?.User?.EmailAddress;
            if (!string.IsNullOrWhiteSpace(staffEmail))
            {
                var staffMessage = new EmailMessage
                {
                    Content = content,
                    To = new List<string> { staffEmail },
                    Subject = subject,
                };

                await _emailer.SendEmailAsync(staffMessage, cancellationToken);

                _logger.LogInformation(
                    "Sent {TemplateType} email to staff {StaffEmail} for appointment {AppointmentCode}",
                    templateType,
                    staffEmail,
                    appointment.Code
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send {TemplateType} notification for appointment {AppointmentId}",
                templateType,
                appointmentId
            );
        }
    }
}
