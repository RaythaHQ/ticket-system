using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using App.Application.Tickets;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Contacts.Queries;

public class GetContactTickets
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TicketListItemDto>>>
    {
        public long ContactId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketListItemDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketListItemDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var tickets = await _db.Tickets
                .AsNoTracking()
                .Include(t => t.Assignee)
                .Include(t => t.OwningTeam)
                .Include(t => t.Contact)
                .Where(t => t.ContactId == request.ContactId)
                .OrderByDescending(t => t.CreationTime)
                .ToListAsync(cancellationToken);

            var dtos = tickets.Select(TicketListItemDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TicketListItemDto>>(dtos);
        }
    }
}

