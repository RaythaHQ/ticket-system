using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.Services;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class CreateAppointment
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long ContactId { get; init; }
        public ShortGuid AppointmentTypeId { get; init; }
        public ShortGuid AssignedStaffMemberId { get; init; }

        /// <summary>
        /// Resolved mode for the appointment. For "either" types, staff chooses "virtual" or "in_person".
        /// For fixed-mode types, must match the type's mode.
        /// </summary>
        public string Mode { get; init; } = null!;

        public string? MeetingLink { get; init; }
        public DateTime ScheduledStartTime { get; init; }
        public int DurationMinutes { get; init; }
        public string? Notes { get; init; }
        public string? CoverageZoneOverrideReason { get; init; }

        // Per-appointment contact fields (pre-populated from Contact, can be overridden)
        public string ContactFirstName { get; init; } = string.Empty;
        public string? ContactLastName { get; init; }
        public string? ContactEmail { get; init; }
        public string? ContactPhone { get; init; }
        public string? ContactAddress { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db, IAvailabilityService availabilityService)
        {
            RuleFor(x => x.ContactId)
                .GreaterThan(0)
                .MustAsync(
                    async (contactId, cancellationToken) =>
                    {
                        return await db
                            .Contacts.AsNoTracking()
                            .AnyAsync(c => c.Id == contactId, cancellationToken);
                    }
                )
                .WithMessage("Contact not found.");

            RuleFor(x => x.AppointmentTypeId)
                .MustAsync(
                    async (typeId, cancellationToken) =>
                    {
                        return await db
                            .AppointmentTypes.AsNoTracking()
                            .AnyAsync(
                                t => t.Id == typeId.Guid && t.IsActive,
                                cancellationToken
                            );
                    }
                )
                .WithMessage("Appointment type not found or inactive.");

            RuleFor(x => x.AssignedStaffMemberId)
                .MustAsync(
                    async (staffId, cancellationToken) =>
                    {
                        return await db
                            .SchedulerStaffMembers.AsNoTracking()
                            .AnyAsync(
                                s => s.Id == staffId.Guid && s.IsActive,
                                cancellationToken
                            );
                    }
                )
                .WithMessage("Staff member not found or inactive.");

            // Staff must be eligible for the appointment type
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        return await db
                            .AppointmentTypeStaffEligibilities.AsNoTracking()
                            .AnyAsync(
                                e =>
                                    e.AppointmentTypeId == cmd.AppointmentTypeId.Guid
                                    && e.SchedulerStaffMemberId
                                        == cmd.AssignedStaffMemberId.Guid,
                                cancellationToken
                            );
                    }
                )
                .WithMessage("Staff member is not eligible for this appointment type.");

            // Mode validation: must match the appointment type's mode
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var appointmentType = await db
                            .AppointmentTypes.AsNoTracking()
                            .FirstOrDefaultAsync(
                                t => t.Id == cmd.AppointmentTypeId.Guid,
                                cancellationToken
                            );

                        if (appointmentType == null)
                            return true; // Already caught by AppointmentTypeId validator

                        var typeMode = appointmentType.Mode;
                        var requestedMode = cmd.Mode?.ToLower();

                        if (typeMode == AppointmentMode.EITHER)
                        {
                            // Staff chooses virtual or in_person
                            return requestedMode == AppointmentMode.VIRTUAL
                                || requestedMode == AppointmentMode.IN_PERSON;
                        }

                        // Fixed mode must match exactly
                        return requestedMode == typeMode;
                    }
                )
                .WithMessage(
                    "Mode must match the appointment type. For 'either' types, choose 'virtual' or 'in_person'."
                );

            // Meeting link required when mode is virtual (unless staff has a default)
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        if (cmd.Mode?.ToLower() != AppointmentMode.VIRTUAL)
                            return true;

                        // If meeting link is provided, it's fine
                        if (!string.IsNullOrWhiteSpace(cmd.MeetingLink))
                            return true;

                        // Check if staff member has a default meeting link
                        var staff = await db
                            .SchedulerStaffMembers.AsNoTracking()
                            .FirstOrDefaultAsync(
                                s => s.Id == cmd.AssignedStaffMemberId.Guid,
                                cancellationToken
                            );

                        return staff != null && !string.IsNullOrWhiteSpace(staff.DefaultMeetingLink);
                    }
                )
                .WithMessage("Meeting link is required for virtual appointments (no default meeting link configured for this staff member).");

            RuleFor(x => x.ScheduledStartTime)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Scheduled start time must be in the future.");

            RuleFor(x => x.DurationMinutes)
                .GreaterThan(0)
                .WithMessage("Duration must be greater than 0.");

            // No time overlap
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        return await availabilityService.IsSlotAvailableAsync(
                            cmd.AssignedStaffMemberId.Guid,
                            cmd.ScheduledStartTime,
                            cmd.DurationMinutes,
                            cancellationToken: cancellationToken
                        );
                    }
                )
                .WithMessage(
                    "The selected time slot is not available. There is a scheduling conflict."
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly IAppointmentCodeGenerator _codeGenerator;
        private readonly ICoverageZoneValidator _coverageZoneValidator;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            IAppointmentCodeGenerator codeGenerator,
            ICoverageZoneValidator coverageZoneValidator,
            ISchedulerPermissionService permissionService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _codeGenerator = codeGenerator;
            _coverageZoneValidator = coverageZoneValidator;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var resolvedMode = request.Mode.ToLower();

            // Resolve meeting link: use provided link, fall back to staff default
            var resolvedMeetingLink = request.MeetingLink;
            if (resolvedMode == AppointmentMode.VIRTUAL && string.IsNullOrWhiteSpace(resolvedMeetingLink))
            {
                var staffMember = await _db
                    .SchedulerStaffMembers.AsNoTracking()
                    .FirstOrDefaultAsync(
                        s => s.Id == request.AssignedStaffMemberId.Guid,
                        cancellationToken
                    );
                resolvedMeetingLink = staffMember?.DefaultMeetingLink;
            }

            // Coverage zone validation for in-person appointments
            if (resolvedMode == AppointmentMode.IN_PERSON)
            {
                var (isValid, warningMessage) =
                    await _coverageZoneValidator.ValidateAsync(
                        request.ContactId,
                        request.AssignedStaffMemberId.Guid,
                        cancellationToken
                    );

                if (!isValid && string.IsNullOrWhiteSpace(request.CoverageZoneOverrideReason))
                {
                    throw new BusinessException(
                        $"Contact is outside the coverage zone. {warningMessage} Provide a CoverageZoneOverrideReason to proceed."
                    );
                }
            }

            var appointmentId = await _codeGenerator.GetNextAppointmentIdAsync(cancellationToken);

            var appointment = new Appointment
            {
                Id = appointmentId,
                ContactId = request.ContactId,
                ContactFirstName = request.ContactFirstName,
                ContactLastName = request.ContactLastName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                ContactAddress = request.ContactAddress,
                AppointmentTypeId = request.AppointmentTypeId.Guid,
                AssignedStaffMemberId = request.AssignedStaffMemberId.Guid,
                Mode = resolvedMode,
                MeetingLink = resolvedMeetingLink,
                ScheduledStartTime = DateTime.SpecifyKind(request.ScheduledStartTime, DateTimeKind.Utc),
                DurationMinutes = request.DurationMinutes,
                Status = AppointmentStatus.SCHEDULED,
                Notes = request.Notes,
                CoverageZoneOverrideReason = request.CoverageZoneOverrideReason,
                CreatedByStaffId = _currentUser.UserId!.Value.Guid,
            };

            // Add creation history entry
            var history = new AppointmentHistory
            {
                AppointmentId = appointmentId,
                ChangeType = "created",
                NewValue = $"Appointment created ({resolvedMode})",
                ChangedByUserId = _currentUser.UserId!.Value.Guid,
            };
            appointment.History.Add(history);

            // Add coverage zone override history if applicable
            if (!string.IsNullOrWhiteSpace(request.CoverageZoneOverrideReason))
            {
                var overrideHistory = new AppointmentHistory
                {
                    AppointmentId = appointmentId,
                    ChangeType = "coverage_override",
                    NewValue = request.CoverageZoneOverrideReason,
                    OverrideReason = request.CoverageZoneOverrideReason,
                    ChangedByUserId = _currentUser.UserId!.Value.Guid,
                };
                appointment.History.Add(overrideHistory);
            }

            appointment.AddDomainEvent(new AppointmentCreatedEvent(appointment));

            _db.Appointments.Add(appointment);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<long>(appointment.Id);
        }
    }
}
