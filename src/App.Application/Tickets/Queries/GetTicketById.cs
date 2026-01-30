using App.Application.Common.Exceptions;
using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTicketById
{
    public record Query : LoggableQuery<IQueryResponseDto<TicketDto>>
    {
        public long Id { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<TicketDto>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<TicketDto>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var ticket = await _db.Tickets
                .AsNoTracking()
                .Include(t => t.OwningTeam)
                .Include(t => t.Assignee)
                .Include(t => t.CreatedByStaff)
                .Include(t => t.Contact)
                .Include(t => t.SlaRule)
                .Include(t => t.SnoozedBy)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (ticket == null)
                throw new NotFoundException("Ticket", request.Id);

            return new QueryResponseDto<TicketDto>(TicketDto.MapFrom(ticket));
        }
    }
}

