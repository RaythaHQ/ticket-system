using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateAppointmentTypeEligibility
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid AppointmentTypeId { get; init; }
        public List<ShortGuid> EligibleStaffMemberIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.AppointmentTypeId)
                .NotEmpty()
                .MustAsync(async (id, cancellationToken) =>
                {
                    return await db.AppointmentTypes
                        .AnyAsync(t => t.Id == id.Guid, cancellationToken);
                })
                .WithMessage("Appointment type not found.");

            RuleFor(x => x.EligibleStaffMemberIds)
                .MustAsync(async (staffIds, cancellationToken) =>
                {
                    if (staffIds == null || staffIds.Count == 0) return true;
                    var staffGuids = staffIds.Select(id => id.Guid).ToList();
                    var activeCount = await db.SchedulerStaffMembers
                        .CountAsync(s => staffGuids.Contains(s.Id) && s.IsActive, cancellationToken);
                    return activeCount == staffIds.Count;
                })
                .WithMessage("All eligible staff members must be active scheduler staff.");
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
            CancellationToken cancellationToken)
        {
            var appointmentTypeId = request.AppointmentTypeId.Guid;

            var entity = await _db.AppointmentTypes
                .FirstOrDefaultAsync(t => t.Id == appointmentTypeId, cancellationToken);

            if (entity == null)
                return new CommandResponseDto<ShortGuid>("AppointmentTypeId", "Appointment type not found.");

            // Full replacement: delete existing eligibility records
            var existingEligibilities = await _db.AppointmentTypeStaffEligibilities
                .Where(e => e.AppointmentTypeId == appointmentTypeId)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingEligibilities)
            {
                _db.AppointmentTypeStaffEligibilities.Remove(existing);
            }

            // Add new junction records
            foreach (var staffId in request.EligibleStaffMemberIds)
            {
                var eligibility = new AppointmentTypeStaffEligibility
                {
                    Id = Guid.NewGuid(),
                    AppointmentTypeId = appointmentTypeId,
                    SchedulerStaffMemberId = staffId.Guid,
                };
                _db.AppointmentTypeStaffEligibilities.Add(eligibility);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
