using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTicketPriorities
{
    public record Query : IRequest<IQueryResponseDto<ListResultDto<TicketPriorityConfigDto>>>
    {
        public bool IncludeInactive { get; init; } = false;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TicketPriorityConfigDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<TicketPriorityConfigDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.TicketPriorityConfigs.AsNoTracking();

            if (!request.IncludeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            var priorities = await query
                .OrderBy(p => p.SortOrder)
                .ToListAsync(cancellationToken);

            var dtos = priorities.Select(TicketPriorityConfigDto.MapFrom).ToList();

            return new QueryResponseDto<ListResultDto<TicketPriorityConfigDto>>(
                new ListResultDto<TicketPriorityConfigDto>(dtos, dtos.Count)
            );
        }
    }
}

