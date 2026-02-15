using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class CreateAppointmentType
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public string Name { get; init; } = null!;
        public string Mode { get; init; } = null!;
        public int? DefaultDurationMinutes { get; init; }
        public int? BufferTimeMinutes { get; init; }
        public int? BookingHorizonDays { get; init; }
        public List<ShortGuid> EligibleStaffIds { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Mode)
                .NotEmpty()
                .Must(mode => AppointmentMode.SupportedTypes.Any(t => t.DeveloperName == mode))
                .WithMessage("Mode must be one of: virtual, in_person, either.");

            RuleFor(x => x.DefaultDurationMinutes)
                .GreaterThan(0)
                .When(x => x.DefaultDurationMinutes.HasValue)
                .WithMessage("Default duration must be a positive number.");

            RuleFor(x => x.BufferTimeMinutes)
                .GreaterThanOrEqualTo(0)
                .When(x => x.BufferTimeMinutes.HasValue)
                .WithMessage("Buffer time must be zero or positive.");

            RuleFor(x => x.BookingHorizonDays)
                .GreaterThan(0)
                .When(x => x.BookingHorizonDays.HasValue)
                .WithMessage("Booking horizon must be a positive number.");

            RuleFor(x => x.EligibleStaffIds)
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
            var appointmentType = new AppointmentType
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Mode = request.Mode,
                DefaultDurationMinutes = request.DefaultDurationMinutes,
                BufferTimeMinutes = request.BufferTimeMinutes,
                BookingHorizonDays = request.BookingHorizonDays,
                IsActive = true,
                SortOrder = 0,
            };

            _db.AppointmentTypes.Add(appointmentType);

            // Create eligibility junction records
            foreach (var staffId in request.EligibleStaffIds)
            {
                var eligibility = new AppointmentTypeStaffEligibility
                {
                    Id = Guid.NewGuid(),
                    AppointmentTypeId = appointmentType.Id,
                    SchedulerStaffMemberId = staffId.Guid,
                };
                _db.AppointmentTypeStaffEligibilities.Add(eligibility);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(appointmentType.Id);
        }
    }
}
