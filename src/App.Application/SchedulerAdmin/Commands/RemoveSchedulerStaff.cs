using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class RemoveSchedulerStaff
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid SchedulerStaffMemberId { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.SchedulerStaffMemberId)
                .NotEmpty()
                .WithMessage("SchedulerStaffMemberId is required.");

            RuleFor(x => x.SchedulerStaffMemberId)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        return await db.SchedulerStaffMembers.AnyAsync(
                            s => s.Id == id.Guid,
                            cancellationToken
                        );
                    }
                )
                .WithMessage("Scheduler staff member not found.");

            RuleFor(x => x.SchedulerStaffMemberId)
                .MustAsync(
                    async (id, cancellationToken) =>
                    {
                        var activeStatuses = new[]
                        {
                            AppointmentStatus.SCHEDULED,
                            AppointmentStatus.CONFIRMED,
                            AppointmentStatus.IN_PROGRESS,
                        };

                        var hasFutureActiveAppointments = await db.Appointments.AnyAsync(
                            a =>
                                a.AssignedStaffMemberId == id.Guid
                                && activeStatuses.Contains(a.Status)
                                && a.ScheduledStartTime > DateTime.UtcNow,
                            cancellationToken
                        );

                        return !hasFutureActiveAppointments;
                    }
                )
                .WithMessage(
                    "Cannot remove staff member with future active appointments. Cancel or reassign them first."
                );
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            var entity = await _db.SchedulerStaffMembers.FirstOrDefaultAsync(
                s => s.Id == request.SchedulerStaffMemberId.Guid,
                cancellationToken
            );

            if (entity == null)
                throw new NotFoundException("SchedulerStaffMember", request.SchedulerStaffMemberId);

            _db.SchedulerStaffMembers.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(request.SchedulerStaffMemberId);
        }
    }
}
