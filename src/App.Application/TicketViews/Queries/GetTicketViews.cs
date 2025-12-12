using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.TicketViews.Queries;

public class GetTicketViews
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TicketViewDto>>>
    {
        /// <summary>
        /// If true, include system views.
        /// </summary>
        public bool IncludeSystem { get; init; } = true;

        /// <summary>
        /// Optional team ID to include team-specific views.
        /// </summary>
        public ShortGuid? TeamId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketViewDto>>>
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(IAppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketViewDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var userId = _currentUser.UserId?.Guid;

            var query = _db.TicketViews
                .AsNoTracking()
                .Include(v => v.OwnerStaff)
                .Where(v =>
                    (v.IsSystem && request.IncludeSystem) || // System views
                    (v.IsDefault && v.OwnerStaffId == null) || // Default views available to all
                    (v.OwnerStaffId == userId) // User's own views
                )
                .OrderBy(v => v.IsSystem ? 0 : 1)
                .ThenBy(v => v.IsDefault ? 0 : 1)
                .ThenBy(v => v.Name);

            var views = await query.ToListAsync(cancellationToken);

            return new QueryResponseDto<IEnumerable<TicketViewDto>>(
                views.Select(TicketViewDto.MapFrom)
            );
        }
    }
}

