using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Commands;

public class DeleteBlockOutTime
{
    public record Command : LoggableRequest<CommandResponseDto<bool>>
    {
        public ShortGuid Id { get; init; }
    }

    public class Handler : IRequestHandler<Command, CommandResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<CommandResponseDto<bool>> Handle(
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

            _db.StaffBlockOutTimes.Remove(blockOut);
            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<bool>(true);
        }
    }
}
