using App.Application.Common.Interfaces;
using App.Application.Common.Models;
using CSharpVitamins;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Teams.Queries;

public class GetTeamMembers
{
    public record Query : IRequest<IQueryResponseDto<IEnumerable<TeamMembershipDto>>>
    {
        public ShortGuid TeamId { get; init; }
    }

    public class Handler : IRequestHandler<Query, IQueryResponseDto<IEnumerable<TeamMembershipDto>>>
    {
        private readonly IAppDbContext _db;

        public Handler(IAppDbContext db)
        {
            _db = db;
        }

        public async ValueTask<IQueryResponseDto<IEnumerable<TeamMembershipDto>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var memberships = await _db.TeamMemberships
                .AsNoTracking()
                .Include(m => m.Team)
                .Include(m => m.StaffAdmin)
                .Where(m => m.TeamId == request.TeamId.Guid)
                .OrderBy(m => m.StaffAdmin.FirstName)
                .ThenBy(m => m.StaffAdmin.LastName)
                .ToListAsync(cancellationToken);

            var dtos = memberships.Select(TeamMembershipDto.MapFrom);

            return new QueryResponseDto<IEnumerable<TeamMembershipDto>>(dtos);
        }
    }
}

