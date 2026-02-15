using System.Text.Json;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateSchedulerConfiguration
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public Dictionary<string, DayScheduleInput> AvailableHours { get; init; } = new();
        public int DefaultDurationMinutes { get; init; } = 30;
        public int DefaultBufferTimeMinutes { get; init; } = 15;
        public int DefaultBookingHorizonDays { get; init; } = 30;
        public int MinCancellationNoticeHours { get; init; } = 24;
        public int ReminderLeadTimeMinutes { get; init; } = 60;
        public List<string>? DefaultCoverageZones { get; init; }
    }

    public record DayScheduleInput
    {
        public string Start { get; init; } = string.Empty;
        public string End { get; init; } = string.Empty;
    }

    private static readonly HashSet<string> ValidDayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"
    };

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DefaultDurationMinutes)
                .GreaterThan(0)
                .WithMessage("Default duration must be a positive number.");

            RuleFor(x => x.DefaultBufferTimeMinutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Default buffer time must be zero or positive.");

            RuleFor(x => x.DefaultBookingHorizonDays)
                .GreaterThan(0)
                .WithMessage("Default booking horizon must be a positive number.");

            RuleFor(x => x.MinCancellationNoticeHours)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum cancellation notice must be zero or positive.");

            RuleFor(x => x.ReminderLeadTimeMinutes)
                .GreaterThan(0)
                .WithMessage("Reminder lead time must be a positive number.");

            RuleFor(x => x.AvailableHours)
                .NotEmpty()
                .WithMessage("At least one day of available hours is required.");

            RuleFor(x => x.AvailableHours)
                .Must(hours => hours.All(kvp => ValidDayNames.Contains(kvp.Key)))
                .WithMessage("Available hours contains invalid day names. Valid values: monday, tuesday, wednesday, thursday, friday, saturday, sunday.");

            RuleForEach(x => x.AvailableHours)
                .Must(kvp =>
                {
                    if (!TimeOnly.TryParse(kvp.Value.Start, out var start)) return false;
                    if (!TimeOnly.TryParse(kvp.Value.End, out var end)) return false;
                    return start < end;
                })
                .WithMessage("Each day's start time must be a valid time before the end time (e.g. 09:00, 17:00).");
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
            var config = await _db.SchedulerConfigurations
                .FirstOrDefaultAsync(cancellationToken);

            if (config == null)
            {
                config = new SchedulerConfiguration { Id = Guid.NewGuid() };
                _db.SchedulerConfigurations.Add(config);
            }

            // Map DayScheduleInput to DaySchedule for the entity
            var hours = request.AvailableHours.ToDictionary(
                kvp => kvp.Key.ToLower(),
                kvp => new DaySchedule { Start = kvp.Value.Start, End = kvp.Value.End });
            config.AvailableHours = hours;

            config.DefaultDurationMinutes = request.DefaultDurationMinutes;
            config.DefaultBufferTimeMinutes = request.DefaultBufferTimeMinutes;
            config.DefaultBookingHorizonDays = request.DefaultBookingHorizonDays;
            config.MinCancellationNoticeHours = request.MinCancellationNoticeHours;
            config.ReminderLeadTimeMinutes = request.ReminderLeadTimeMinutes;

            config.DefaultCoverageZones = request.DefaultCoverageZones ?? new List<string>();

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(config.Id);
        }
    }
}
