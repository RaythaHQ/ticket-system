using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketTasks.Queries;

public class GetTasksByTicketId
{
    public record Query : LoggableQuery<IQueryResponseDto<IEnumerable<TicketTaskDto>>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketTaskDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketTaskDto>>> Handle(
            Query request,
            CancellationToken cancellationToken)
        {
            var tasks = await _db.TicketTasks
                .AsNoTracking()
                .Where(t => t.TicketId == request.TicketId)
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.DependsOnTask)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.ClosedByStaff)
                .OrderBy(t => t.SortOrder)
                .ToListAsync(cancellationToken);

            var dtos = tasks.Select(TicketTaskDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TicketTaskDto>>(dtos);
        }
    }
}
