using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Scheduler.DTOs;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Scheduler.Queries;

public class GetBlockOutTimes
{
    public record Query : IRequest<IQueryResponseDto<List<BlockOutTimeDto>>>, ILoggableQuery
    {
        /// <summary>
        /// Filter by specific staff member IDs. If empty, returns block-outs for all staff.
        /// </summary>
        public List<ShortGuid> StaffMemberIds { get; init; } = new();

        /// <summary>
        /// Start of the date range to query.
        /// </summary>
        public DateTime DateFrom { get; init; }

        /// <summary>
        /// End of the date range to query.
        /// </summary>
        public DateTime DateTo { get; init; }

        public string GetLogName() => "Scheduler.Queries.GetBlockOutTimes";
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<List<BlockOutTimeDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ISchedulerPermissionService _permissionService;

        public Handler(IAppDbContext db, ISchedulerPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async ValueTask<IQueryResponseDto<List<BlockOutTimeDto>>> Handle(
            Query request, CancellationToken cancellationToken)
        {
            await _permissionService.RequireIsSchedulerStaffAsync(cancellationToken);

            var dateFrom = DateTime.SpecifyKind(request.DateFrom, DateTimeKind.Utc);
            var dateTo = DateTime.SpecifyKind(request.DateTo, DateTimeKind.Utc);

            var query = _db.StaffBlockOutTimes.AsNoTracking()
                .Include(b => b.StaffMember)
                    .ThenInclude(s => s.User)
                .Where(b => b.StartTimeUtc < dateTo && b.EndTimeUtc > dateFrom);

            if (request.StaffMemberIds.Any())
            {
                var staffGuids = request.StaffMemberIds.Select(s => s.Guid).ToList();
                query = query.Where(b => staffGuids.Contains(b.StaffMemberId));
            }

            var blockOuts = await query
                .OrderBy(b => b.StartTimeUtc)
                .ToListAsync(cancellationToken);

            var dtos = blockOuts.Select(BlockOutTimeDto.MapFrom).ToList();

            return new QueryResponseDto<List<BlockOutTimeDto>>(dtos);
        }
    }
}
