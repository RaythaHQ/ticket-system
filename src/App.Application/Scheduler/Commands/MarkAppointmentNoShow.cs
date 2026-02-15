using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.Events;
using App.Domain.ValueObjects;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class MarkAppointmentNoShow
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long AppointmentId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
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
                .WithMessage("Only active appointments can be marked as no-show.");
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
                throw new BusinessException(
                    "Only active appointments can be marked as no-show."
                );

            var oldStatus = appointment.Status;
            appointment.Status = AppointmentStatus.NO_SHOW;

            // Create history entry
            var history = new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ChangeType = "status_changed",
                OldValue = oldStatus,
                NewValue = AppointmentStatus.NO_SHOW,
                ChangedByUserId = _currentUser.UserId!.Value.Guid,
            };
            _db.AppointmentHistories.Add(history);

            appointment.AddDomainEvent(
                new AppointmentStatusChangedEvent(
                    appointment,
                    oldStatus,
                    AppointmentStatus.NO_SHOW,
                    _currentUser.UserId!.Value.Guid
                )
            );

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(appointment.Id);
        }
    }
}
