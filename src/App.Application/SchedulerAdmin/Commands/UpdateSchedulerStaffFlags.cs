using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.SchedulerAdmin.Commands;

public class UpdateSchedulerStaffFlags
{
    public record Command : LoggableRequest<CommandResponseDto<ShortGuid>>
    {
        public ShortGuid SchedulerStaffMemberId { get; init; }
        public bool CanManageOthersCalendars { get; init; }
        public string? DefaultMeetingLink { get; init; }
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

            entity.CanManageOthersCalendars = request.CanManageOthersCalendars;
            entity.DefaultMeetingLink = string.IsNullOrWhiteSpace(request.DefaultMeetingLink)
                ? null
                : request.DefaultMeetingLink.Trim();

            await _db.SaveChangesAsync(cancellationToken);

            return new CommandResponseDto<ShortGuid>(entity.Id);
        }
    }
}
