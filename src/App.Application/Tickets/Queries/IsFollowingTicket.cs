using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class IsFollowingTicket
{
    public record Query : IRequest<IQueryResponseDto<bool>>
    {
        public long TicketId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<bool>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<bool>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (_currentUser.UserId == null)
                return new QueryResponseDto<bool>(false);

            var isFollowing = await _db
                .TicketFollowers.AsNoTracking()
                .AnyAsync(
                    f =>
                        f.TicketId == request.TicketId
                        && f.StaffAdminId == _currentUser.UserId.Value.Guid,
                    cancellationToken
                );

            return new QueryResponseDto<bool>(isFollowing);
        }
    }
}
