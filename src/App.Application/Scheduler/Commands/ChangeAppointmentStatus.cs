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

public class ChangeAppointmentStatus
{
    public record Command : LoggableRequest<CommandResponseDto<long>>
    {
        public long AppointmentId { get; init; }
        public string NewStatus { get; init; } = null!;
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

            RuleFor(x => x.NewStatus)
                .NotEmpty()
                .Must(status =>
                {
                    try
                    {
                        AppointmentStatus.From(status);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .WithMessage("Invalid appointment status.");

            // Validate that the transition is allowed
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
                            return true; // Already caught by existence check

                        try
                        {
                            var currentStatus = AppointmentStatus.From(appointment.Status);
                            var targetStatus = AppointmentStatus.From(cmd.NewStatus);
                            return currentStatus.CanTransitionTo(targetStatus);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                )
                .WithMessage(
                    "Invalid status transition. Check the current status and allowed transitions."
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

            var oldStatus = appointment.Status;
            var newStatusLower = request.NewStatus.ToLower();
            var currentStatusValue = AppointmentStatus.From(oldStatus);
            var targetStatusValue = AppointmentStatus.From(newStatusLower);

            if (!currentStatusValue.CanTransitionTo(targetStatusValue))
            {
                throw new BusinessException(
                    $"Cannot transition from '{currentStatusValue.Label}' to '{targetStatusValue.Label}'."
                );
            }

            appointment.Status = newStatusLower;

            // Create history entry
            var history = new AppointmentHistory
            {
                AppointmentId = appointment.Id,
                ChangeType = "status_changed",
                OldValue = oldStatus,
                NewValue = newStatusLower,
                ChangedByUserId = _currentUser.UserId!.Value.Guid,
            };
            _db.AppointmentHistories.Add(history);

            // Raise status changed event
            appointment.AddDomainEvent(
                new AppointmentStatusChangedEvent(
                    appointment,
                    oldStatus,
                    newStatusLower,
                    _currentUser.UserId!.Value.Guid
                )
            );

            // If completed, also raise completed event
            if (newStatusLower == AppointmentStatus.COMPLETED)
            {
                appointment.AddDomainEvent(new AppointmentCompletedEvent(appointment));
            }

            await _db.SaveChangesAsync(cancellationToken);
            return new CommandResponseDto<long>(appointment.Id);
        }
    }
}
