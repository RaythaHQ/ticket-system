using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class UpdateBlockOutTime
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public DateTime StartTimeUtc { get; init; }
        public DateTime EndTimeUtc { get; init; }
        public bool IsAllDay { get; init; }
        public string? Reason { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(250);

            RuleFor(x => x.EndTimeUtc)
                .GreaterThan(x => x.StartTimeUtc)
                .WithMessage("End time must be after start time.");

            RuleFor(x => x.Reason).MaximumLength(1000);
        }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<ShortGuid>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<ShortGuid>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var blockOut = await _db.StaffBlockOutTimes
                .FirstOrDefaultAsync(b => b.Id == request.Id.Guid, cancellationToken)
                ?? throw new NotFoundException("Block out time", request.Id.Guid);

            var currentStaffId = await _permissionService.GetCurrentStaffMemberIdAsync(cancellationToken);
            if (currentStaffId.HasValue && currentStaffId.Value != blockOut.StaffMemberId)
            {
                var currentStaff = await _db.SchedulerStaffMembers.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == currentStaffId.Value, cancellationToken);

                if (currentStaff == null || !currentStaff.CanManageOthersCalendars)
                {
                    throw new ForbiddenAccessException(
                        "You do not have permission to manage this staff member's schedule.");
                }
            }

            blockOut.Title = request.Title;
            blockOut.StartTimeUtc = DateTime.SpecifyKind(request.StartTimeUtc, DateTimeKind.Utc);
            blockOut.EndTimeUtc = DateTime.SpecifyKind(request.EndTimeUtc, DateTimeKind.Utc);
            blockOut.IsAllDay = request.IsAllDay;
            blockOut.Reason = request.Reason;

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(blockOut.Id);
        }
    }
}
