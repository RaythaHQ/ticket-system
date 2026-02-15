using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.Services;
using App.Domain.Entities;
using App.Domain.Events;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class RescheduleAppointment
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long AppointmentId { get; init; }
        public DateTime NewScheduledStartTime { get; init; }
        public int NewDurationMinutes { get; init; }
        public string? CancellationNoticeOverrideReason { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db, IAvailabilityService availabilityService)
        {
            RuleFor(x => x.AppointmentId)
                .GreaterThan(0)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        return await db
                            .Appointments.AsNoTracking()
                            .AnyAsync(a => a.Id == id, cancellationToken);
                    }
                )
                .WithMessage("Appointment not found.");

            // Appointment must be active
            RuleFor(x => x.AppointmentId)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        var appointment = await db
                            .Appointments.AsNoTracking()
                            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

                        if (appointment == null)
                            return true;

                        return appointment.StatusValue.IsActive;
                    }
                )
                .WithMessage("Only active appointments can be rescheduled.");

            RuleFor(x => x.NewScheduledStartTime)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("New scheduled start time must be in the future.");

            RuleFor(x => x.NewDurationMinutes)
                .GreaterThan(0)
                .WithMessage("Duration must be greater than 0.");

            // No overlap at new time (exclude current appointment)
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var appointment = await db
                            .Appointments.AsNoTracking()
                            .FirstOrDefaultAsync(
                                a => a.Id == cmd.AppointmentId,
                                cancellationToken
                            );

                        if (appointment == null)
                            return true;

                        return await availabilityService.IsSlotAvailableAsync(
                            appointment.AssignedStaffMemberId,
                            cmd.NewScheduledStartTime,
                            cmd.NewDurationMinutes,
                            excludeAppointmentId: cmd.AppointmentId,
                            cancellationToken: cancellationToken
                        );
                    }
                )
                .WithMessage(
                    "The new time slot is not available. There is a scheduling conflict."
                );

            // Cancellation notice check
            RuleFor(x => x)
                .MustAsync(
                    async (cmd, cancellationToken) =>
                    {
                        var appointment = await db
                            .Appointments.AsNoTracking()
                            .FirstOrDefaultAsync(
                                a => a.Id == cmd.AppointmentId,
                                cancellationToken
                            );

                        if (appointment == null)
                            return true;

                        var config = await db
                            .SchedulerConfigurations.AsNoTracking()
                            .FirstOrDefaultAsync(cancellationToken);

                        if (config == null)
                            return true;

                        var hoursUntilAppointment = (
                            appointment.ScheduledStartTime - DateTime.UtcNow
                        ).TotalHours;

                        if (hoursUntilAppointment < config.MinCancellationNoticeHours)
                        {
                            // Within notice period — override reason required
                            return !string.IsNullOrWhiteSpace(
                                cmd.CancellationNoticeOverrideReason
                            );
                        }

                        return true;
                    }
                )
                .WithMessage(
                    "Rescheduling within the minimum cancellation notice period requires a CancellationNoticeOverrideReason."
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<long>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ISchedulerPermissionService permissionService
        )
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<long>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var appointment = await _db.Appointments.FirstOrDefaultAsync(
                a => a.Id == request.AppointmentId,
                cancellationToken
            );

            if (appointment == null)
                throw new NotFoundException("Appointment", request.AppointmentId);

            if (!appointment.StatusValue.IsActive)
                throw new BusinessException("Only active appointments can be rescheduled.");

            // Save old values
            var oldStartTime = appointment.ScheduledStartTime;
            var oldDurationMinutes = appointment.DurationMinutes;

            // Update schedule
            appointment.ScheduledStartTime = DateTime.SpecifyKind(request.NewScheduledStartTime, DateTimeKind.Utc);
            appointment.DurationMinutes = request.NewDurationMinutes;
            appointment.CancellationNoticeOverrideReason =
                request.CancellationNoticeOverrideReason;

            // Create history entry
            var history = new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ChangeType = "rescheduled",
                OldValue =
                    $"{oldStartTime:O} ({oldDurationMinutes} min)",
                NewValue =
                    $"{request.NewScheduledStartTime:O} ({request.NewDurationMinutes} min)",
                ChangedByUserId = _currentUser.UserId!.Value.Guid,
            };
            _db.AppointmentHistories.Add(history);

            // Add cancellation notice override history if applicable
            if (!string.IsNullOrWhiteSpace(request.CancellationNoticeOverrideReason))
            {
                var overrideHistory = new AppointmentHistory
                {
                    AppointmentId = appointment.Id,
                    ChangeType = "cancellation_notice_override",
                    OverrideReason = request.CancellationNoticeOverrideReason,
                    ChangedByUserId = _currentUser.UserId!.Value.Guid,
                };
                _db.AppointmentHistories.Add(overrideHistory);
            }

            appointment.AddDomainEvent(
                new AppointmentRescheduledEvent(
                    appointment,
                    oldStartTime,
                    request.NewScheduledStartTime,
                    oldDurationMinutes,
                    request.NewDurationMinutes
                )
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(appointment.Id);
        }
    }
}
