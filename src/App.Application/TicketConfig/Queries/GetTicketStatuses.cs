using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketConfig.Queries;

public class GetTicketStatuses
{
    public record Query : IRequest<IQueryResponseDto<ListResultDto<TicketStatusConfigDto>>>
    {
        public bool IncludeInactive { get; init; } = false;
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<ListResultDto<TicketStatusConfigDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<ListResultDto<TicketStatusConfigDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = _db.TicketStatusConfigs.AsNoTracking();

            if (!request.IncludeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var statuses = await query
                .OrderBy(s => s.SortOrder)
                .ToListAsync(cancellationToken);

            var dtos = statuses.Select(TicketStatusConfigDto.MapFrom).ToList();

            return new QueryResponseDto<ListResultDto<TicketStatusConfigDto>>(
                new ListResultDto<TicketStatusConfigDto>(dtos, dtos.Count)
            );
        }
    }
}

