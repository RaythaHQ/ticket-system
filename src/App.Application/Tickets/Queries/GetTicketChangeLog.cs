using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTicketChangeLog
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TicketChangeLogEntryDto>>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketChangeLogEntryDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketChangeLogEntryDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var entries = await _db.TicketChangeLogEntries
                .AsNoTracking()
                .Include(e => e.ActorStaff)
                .Where(e => e.TicketId == request.TicketId)
                .OrderByDescending(e => e.CreationTime)
                .ToListAsync(cancellationToken);

            var dtos = entries.Select(TicketChangeLogEntryDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TicketChangeLogEntryDto>>(dtos);
        }
    }
}

