using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Notifications.Queries;

public class GetUnreadNotificationCount
{
    public record Query : IRequest<IQueryResponseDto<int>> { }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<int>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<int>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId?.Guid;
            if (!userId.HasValue)
            {
                return new QueryResponseDto<int>(0);
            }

            var count = await _db.Notifications.AsNoTracking()
                .CountAsync(n => n.RecipientUserId == userId.Value && !n.IsRead, cancellationToken);

            return new QueryResponseDto<int>(count);
        }
    }
}

