using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.Services;
using App.Domain.Entities;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class CreateBlockOutTime
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid StaffMemberId { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime StartTimeUtc { get; init; }
        public DateTime EndTimeUtc { get; init; }
        public bool IsAllDay { get; init; }
        public string? Reason { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IAppDbContext db)
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(250);

            RuleFor(x => x.EndTimeUtc)
                .GreaterThan(x => x.StartTimeUtc)
                .WithMessage("End time must be after start time.");

            RuleFor(x => x.StaffMemberId)
                .MustAsync(async (staffId, ct) =>
                    await db.SchedulerStaffMembers.AsNoTracking()
                        .AnyAsync(s => s.Id == staffId.Guid && s.IsActive, ct))
                .WithMessage("Staff member not found or inactive.");

            RuleFor(x => x.Reason).MaximumLength(1000);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(
            IAppDbContext db,
            ICurrentUser currentUser,
            ISchedulerPermissionService permissionService)
        {
            _db = db;
            _currentUser = currentUser;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var currentStaffId = await _permissionService.GetCurrentStaffMemberIdAsync(cancellationToken);

            // If creating for someone else, must have CanManageOthersCalendars
            if (currentStaffId.HasValue && currentStaffId.Value != request.StaffMemberId.Guid)
            {
                var currentStaff = await _db.SchedulerStaffMembers.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == currentStaffId.Value, cancellationToken);

                if (currentStaff == null || !currentStaff.CanManageOthersCalendars)
                {
                    throw new Common.Exceptions.ForbiddenAccessException(
                        "You do not have permission to manage this staff member's schedule.");
                }
            }

            var blockOut = new StaffBlockOutTime
            {
                StaffMemberId = request.StaffMemberId.Guid,
                Title = request.Title,
                StartTimeUtc = DateTime.SpecifyKind(request.StartTimeUtc, DateTimeKind.Utc),
                EndTimeUtc = DateTime.SpecifyKind(request.EndTimeUtc, DateTimeKind.Utc),
                IsAllDay = request.IsAllDay,
                Reason = request.Reason,
            };

            _db.StaffBlockOutTimes.Add(blockOut);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(blockOut.Id);
        }
    }
}
