using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Tickets.Queries;

public class GetTicketFollowers
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TicketFollowerDto>>>
    {
        public long TicketId { get; init; }
    }

    public record TicketFollowerDto
    {
        public ShortGuid Id { get; init; }
        public ShortGuid StaffAdminId { get; init; }
        public string StaffAdminName { get; init; } = null!;
        public DateTime FollowedAt { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TicketFollowerDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TicketFollowerDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var followers = await _db
                .TicketFollowers.AsNoTracking()
                .Where(f => f.TicketId == request.TicketId)
                .Include(f => f.StaffAdmin)
                .OrderBy(f => f.CreationTime)
                .Select(f => new TicketFollowerDto
                {
                    Id = f.Id,
                    StaffAdminId = f.StaffAdminId,
                    StaffAdminName = f.StaffAdmin.FirstName + " " + f.StaffAdmin.LastName,
                    FollowedAt = f.CreationTime,
                })
                .ToListAsync(cancellationToken);

            return new QueryResponseDto<IEnumerable<TicketFollowerDto>>(followers);
        }
    }
}
