using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;

namespace App.Application.Scheduler.DTOs;

/// <summary>
/// Full detail DTO for an appointment (staff view).
/// </summary>
public record AppointmentDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public long ContactId { get; init; }
    public string ContactName { get; init; } = string.Empty;
    public string? ContactEmail { get; init; }
    public string? ContactZipcode { get; init; }

    // Per-appointment contact fields (snapshot, may differ from Contact record)
    public string AppointmentContactFirstName { get; init; } = string.Empty;
    public string? AppointmentContactLastName { get; init; }
    public string? AppointmentContactEmail { get; init; }
    public string? AppointmentContactPhone { get; init; }
    public string? AppointmentContactAddress { get; init; }
    public ShortGuid AssignedStaffMemberId { get; init; }
    public string AssignedStaffName { get; init; } = string.Empty;
    public string AssignedStaffEmail { get; init; } = string.Empty;
    public ShortGuid AppointmentTypeId { get; init; }
    public string AppointmentTypeName { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
    public string? MeetingLink { get; init; }
    public DateTime ScheduledStartTime { get; init; }
    public int DurationMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public string? CancellationReason { get; init; }
    public string? CoverageZoneOverrideReason { get; init; }
    public string? CancellationNoticeOverrideReason { get; init; }
    public DateTime? ReminderSentAt { get; init; }
    public string CreatedByStaffName { get; init; } = string.Empty;
    public DateTime CreationTime { get; init; }
    public DateTime? LastModificationTime { get; init; }
    public List<AppointmentHistoryItemDto> History { get; init; } = new();

    /// <summary>
    /// Valid status transitions from the current status.
    /// </summary>
    public List<string> AllowedStatusTransitions { get; init; } = new();

    public static AppointmentDto MapFrom(Appointment appointment)
    {
        var statusValue = AppointmentStatus.From(appointment.Status);
        return new AppointmentDto
        {
            Id = appointment.Id,
            Code = appointment.Code,
            ContactId = appointment.ContactId,
            ContactName = appointment.Contact?.FullName ?? string.Empty,
            ContactEmail = appointment.Contact?.Email,
            ContactZipcode = appointment.Contact?.Zipcode,
            AppointmentContactFirstName = appointment.ContactFirstName,
            AppointmentContactLastName = appointment.ContactLastName,
            AppointmentContactEmail = appointment.ContactEmail,
            AppointmentContactPhone = appointment.ContactPhone,
            AppointmentContactAddress = appointment.ContactAddress,
            AssignedStaffMemberId = appointment.AssignedStaffMemberId,
            AssignedStaffName = appointment.AssignedStaffMember?.User != null
                ? appointment.AssignedStaffMember.User.FirstName
                    + " "
                    + appointment.AssignedStaffMember.User.LastName
                : string.Empty,
            AssignedStaffEmail =
                appointment.AssignedStaffMember?.User?.EmailAddress ?? string.Empty,
            AppointmentTypeId = appointment.AppointmentTypeId,
            AppointmentTypeName = appointment.AppointmentType?.Name ?? string.Empty,
            Mode = appointment.Mode,
            ModeLabel = AppointmentMode.From(appointment.Mode).Label,
            MeetingLink = appointment.MeetingLink,
            ScheduledStartTime = appointment.ScheduledStartTime,
            DurationMinutes = appointment.DurationMinutes,
            Status = appointment.Status,
            StatusLabel = statusValue.Label,
            Notes = appointment.Notes,
            CancellationReason = appointment.CancellationReason,
            CoverageZoneOverrideReason = appointment.CoverageZoneOverrideReason,
            CancellationNoticeOverrideReason = appointment.CancellationNoticeOverrideReason,
            ReminderSentAt = appointment.ReminderSentAt,
            CreatedByStaffName = appointment.CreatedByStaff != null
                ? appointment.CreatedByStaff.FirstName + " " + appointment.CreatedByStaff.LastName
                : string.Empty,
            CreationTime = appointment.CreationTime,
            LastModificationTime = appointment.LastModificationTime,
            History = appointment
                .History?.OrderByDescending(h => h.Timestamp)
                .Select(AppointmentHistoryItemDto.MapFrom)
                .ToList()
                ?? new List<AppointmentHistoryItemDto>(),
            AllowedStatusTransitions = AppointmentStatus
                .SupportedTypes.Where(s => statusValue.CanTransitionTo(s))
                .Select(s => s.DeveloperName)
                .ToList(),
        };
    }
}

/// <summary>
/// Lightweight DTO for appointment list views.
/// </summary>
public record AppointmentListItemDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string ContactName { get; init; } = string.Empty;
    public long ContactId { get; init; }
    public string AssignedStaffName { get; init; } = string.Empty;
    public string AppointmentTypeName { get; init; } = string.Empty;
    public string Mode { get; init; } = string.Empty;
    public string ModeLabel { get; init; } = string.Empty;
    public DateTime ScheduledStartTime { get; init; }
    public int DurationMinutes { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public DateTime CreationTime { get; init; }

    public static AppointmentListItemDto MapFrom(Appointment appointment)
    {
        return new AppointmentListItemDto
        {
            Id = appointment.Id,
            Code = appointment.Code,
            ContactName = appointment.Contact?.FullName ?? string.Empty,
            ContactId = appointment.ContactId,
            AssignedStaffName = appointment.AssignedStaffMember?.User != null
                ? appointment.AssignedStaffMember.User.FirstName
                    + " "
                    + appointment.AssignedStaffMember.User.LastName
                : string.Empty,
            AppointmentTypeName = appointment.AppointmentType?.Name ?? string.Empty,
            Mode = appointment.Mode,
            ModeLabel = AppointmentMode.From(appointment.Mode).Label,
            ScheduledStartTime = appointment.ScheduledStartTime,
            DurationMinutes = appointment.DurationMinutes,
            Status = appointment.Status,
            StatusLabel = AppointmentStatus.From(appointment.Status).Label,
            CreationTime = appointment.CreationTime,
        };
    }
}

/// <summary>
/// DTO for appointment history entries.
/// </summary>
public record AppointmentHistoryItemDto
{
    public Guid Id { get; init; }
    public string ChangeType { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public string? OverrideReason { get; init; }
    public string ChangedByName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }

    public static AppointmentHistoryItemDto MapFrom(AppointmentHistory history)
    {
        return new AppointmentHistoryItemDto
        {
            Id = history.Id,
            ChangeType = history.ChangeType,
            OldValue = history.OldValue,
            NewValue = history.NewValue,
            OverrideReason = history.OverrideReason,
            ChangedByName = history.ChangedByUser != null
                ? history.ChangedByUser.FirstName + " " + history.ChangedByUser.LastName
                : string.Empty,
            Timestamp = history.Timestamp,
        };
    }
}
