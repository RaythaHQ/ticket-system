using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTicketComments
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TicketCommentDto>>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketCommentDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketCommentDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var comments = await _db.TicketComments
                .AsNoTracking()
                .Include(c => c.AuthorStaff)
                .Where(c => c.TicketId == request.TicketId)
                .OrderByDescending(c => c.CreationTime)
                .ToListAsync(cancellationToken);

            var dtos = comments.Select(TicketCommentDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TicketCommentDto>>(dtos);
        }
    }
}

