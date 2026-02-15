using System.Text.Json;
using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Common.Utils;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateStaffAvailability
{
    public record DayScheduleInput
    {
        public string Start { get; init; } = string.Empty;
        public string End { get; init; } = string.Empty;
    }

    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid SchedulerStaffMemberId { get; init; }
        public Dictionary<string, DayScheduleInput> Availability { get; init; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> ValidDays = new(StringComparer.OrdinalIgnoreCase)
        {
            "monday",
            "tuesday",
            "wednesday",
            "thursday",
            "friday",
            "saturday",
            "sunday",
        };

        public Validator()
        {
            RuleFor(x => x.SchedulerStaffMemberId)
                .NotEmpty()
                .WithMessage("SchedulerStaffMemberId is required.");

            RuleForEach(x => x.Availability)
                .Must(kvp => ValidDays.Contains(kvp.Key))
                .WithMessage("Invalid day name. Must be monday through sunday.");

            RuleForEach(x => x.Availability)
                .Must(kvp =>
                {
                    if (
                        !TimeOnly.TryParse(kvp.Value.Start, out var start)
                        || !TimeOnly.TryParse(kvp.Value.End, out var end)
                    )
                    {
                        return false;
                    }

                    return start < end;
                })
                .WithMessage(
                    "Each day's start time must be a valid time and must be before the end time."
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

            // Convert DayScheduleInput to DaySchedule for serialization
            var availability = request.Availability.ToDictionary(
                kvp => kvp.Key.ToLower(),
                kvp => new DaySchedule { Start = kvp.Value.Start, End = kvp.Value.End }
            );

            entity.Availability = availability;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
