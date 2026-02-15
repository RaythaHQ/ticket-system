using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.ValueObjects;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateAppointmentType
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public required ShortGuid Id { get; init; }
        public string Name { get; init; } = null!;
        public string Mode { get; init; } = null!;
        public int? DefaultDurationMinutes { get; init; }
        public int? BufferTimeMinutes { get; init; }
        public int? BookingHorizonDays { get; init; }
        public bool IsActive { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Id).NotEmpty();

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

            RuleFor(x => x.Id)
                .MustAsync(async (id, cancellationToken) =>
                {
                    return await db.AppointmentTypes
                        .AnyAsync(t => t.Id == id.Guid, cancellationToken);
                })
                .WithMessage("Appointment type not found.");
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
            var entity = await _db.AppointmentTypes
                .FirstOrDefaultAsync(t => t.Id == request.Id.Guid, cancellationToken);

            if (entity == null)
                return new CommandResponseDto<ShortGuid>("Id", "Appointment type not found.");

            entity.Name = request.Name;
            entity.Mode = request.Mode;
            entity.DefaultDurationMinutes = request.DefaultDurationMinutes;
            entity.BufferTimeMinutes = request.BufferTimeMinutes;
            entity.BookingHorizonDays = request.BookingHorizonDays;
            entity.IsActive = request.IsActive;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
